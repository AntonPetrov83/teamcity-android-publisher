#r "nuget:Google.Apis.AndroidPublisher.v3, 1.57.0"

using Google.Apis.AndroidPublisher.v3;
using Google.Apis.AndroidPublisher.v3.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;

string packageName = null;
string apkFile = null;
string obbFile = null;

AndroidPublisherService service;
AppEdit edit;
Apk apk;
ExpansionFile expansionFile;
        
try
{
    Run(Args.ToArray()).Wait();

    return 0;
}
catch (AggregateException ex)
{
    foreach (var e in ex.InnerExceptions)
    {
        Console.WriteLine("ERROR: " + e.Message);
    }

    return 1;
}
        
async Task Run(string[] args)
{
    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--package-name":
                packageName = args[++i];
                break;

            case "--apk-file":
                apkFile = args[++i];
                break;
            
            case "--obb-file":
                obbFile = args[++i];
                break;
        }
    }

    if (string.IsNullOrEmpty(packageName))
    {
        throw new Exception("--package-name required");
    }
    
    if (string.IsNullOrEmpty(apkFile))
    {
        throw new Exception("--apk-file required");
    }

    var googleApiJson = Environment.GetEnvironmentVariable("GOOGLE_API_JSON");
    
    var credential = GoogleCredential
        .FromJson(googleApiJson)
        .CreateScoped(AndroidPublisherService.Scope.Androidpublisher);

    service = new AndroidPublisherService(
        new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential
        });                
    
    edit = await service.Edits.Insert(new AppEdit(), packageName).ExecuteAsync();

    apk = await UploadApkAsync();
    
    if (!string.IsNullOrEmpty(obbFile))
    {
        expansionFile = await UploadObbAsync();
    }

    var track = new Track
    {
        Releases = new List<TrackRelease>
        {
            new TrackRelease
            {
                VersionCodes = new List<long?> { apk.VersionCode },
                Status = "completed"
            }
        }
    };

    Console.WriteLine("updating 'internal' track");
    await service.Edits.Tracks.Update(track, packageName, edit.Id, "internal").ExecuteAsync();

    Console.WriteLine("commiting changes");
    await service.Edits.Commit(packageName, edit.Id).ExecuteAsync();
}

async Task<Apk> UploadApkAsync()
{
    Console.WriteLine("uploading APK file: " + apkFile);

    await using var stream = File.OpenRead(apkFile);
    var upload = service.Edits.Apks.Upload(packageName, edit.Id, stream, "application/octet-stream");
    var uploadProgress = await upload.UploadAsync();

    return uploadProgress.Status switch
    {
        UploadStatus.Completed => upload.ResponseBody,
        _ => throw new Exception("upload failed")
    };
}
        
async Task<ExpansionFile> UploadObbAsync()
{
    Console.WriteLine("uploading OBB file: " + obbFile);

    if (apk?.VersionCode == null)
        throw new Exception("missing APK file or invalid APK version code");
    
    await using var stream = File.OpenRead(obbFile);
    var upload = service.Edits.Expansionfiles.Upload(packageName, edit.Id, apk.VersionCode.Value,
        EditsResource.ExpansionfilesResource.UploadMediaUpload.ExpansionFileTypeEnum.Main, stream,
        "application/octet-stream");
    var uploadProgress = await upload.UploadAsync();
    
    return uploadProgress.Status switch
    {
        UploadStatus.Completed => upload.ResponseBody.ExpansionFile,
        _ => throw new Exception("upload failed")
    };
}
