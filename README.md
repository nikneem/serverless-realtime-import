# Getting real-time insights from your Serverless Solutions

This is a demo I'm running on my talk 'Getting real-time insights from your Serverless Solutions' during conferences and software development events. This solution contains two projects, one Azure Functions project and one Angular front-end project.

## To run the serverless

Download the source code and open the solution in Visual Studio .NET 2019 (v 16.4.3 or higher, the serverless project runs Azure Fuctions 3.0). Go to your azure portal and create a new SignalR service, make sure the Server Mode is set to Serverless!! Open the properties of your SignalR Service and view the Keys properties. Copy one of the connection strings. Now go back to Visual Studio and create a local settings file (local.settings.json) in the root of your project and add the following:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "AzureSignalRConnectionString": "/--your-signalr-connectionstring--/",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet"
  },
  "Host": {
    "LocalHttpPort": 7071,
    "CORS": "http://localhost:4200",
    "CORSCredentials": true
  }
}
```

Note you need to replace one part with the connection string you copied from Azure. Also note the 'Host' section. This part is required for the SignalR Service to run properly. You can now run the serverless project.

As you can see, the project runs on a local storage account. You can replace this with an online version if you like.

## To run the Angular project

Make sure you have Node JS > 10 installed. Download the source code and open
a console window.
To restore all packages

```bash
npm i
```

and to run the project

```bash
ng serve -o
```

When the project runs, you should see (part of) the Azure Functions logo with transparency.

## Now you know both projects work

Now to get a demonstration of how the async import with rea-time insights works, create a container in your BLOB Storage called import.
Run the serverless project (if not already running, you may also restart it just to be sure). Go to the front-end (Angular) website and refresh it, just to make sure the SignalR connection is established. Then start uploading files to the import container. In case the system doesn't recognize the file you will see an error message. In case us use one of the demo import files, it starts importing that data providing feedback of it's progress.

Have Fun!
