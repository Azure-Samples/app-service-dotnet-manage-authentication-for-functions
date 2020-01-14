---
page_type: sample
languages:
- csharp
products:
- azure
extensions:
- services: App-Service
- platforms: dotnet
---

# Getting started on managing authentication for Functions using C# #

 Azure App Service basic sample for managing function apps.
  - Create 3 function apps under the same new app service plan and with the same storage account
    - Deploy 1 and 2 via Git a function that calculates the square of a number
    - Deploy 3 via Web Deploy
    - Enable app level authentication for the 1st function app
    - Verify the 1st function app can be accessed with the admin key
    - Enable function level authentication for the 2nd function app
    - Verify the 2nd function app can be accessed with the function key
    - Enable function level authentication for the 3rd function app
    - Verify the 3rd function app can be accessed with the function key


## Running this Sample ##

To run this sample:

Set the environment variable `AZURE_AUTH_LOCATION` with the full path for an auth file. See [how to create an auth file](https://github.com/Azure/azure-libraries-for-net/blob/master/AUTH.md).

    git clone https://github.com/Azure-Samples/app-service-dotnet-manage-authentication-for-functions.git

    cd app-service-dotnet-manage-authentication-for-functions

    dotnet build

    bin\Debug\net452\ManageFunctionAppWithAuthentication.exe

## More information ##

[Azure Management Libraries for C#](https://github.com/Azure/azure-sdk-for-net/tree/Fluent)
[Azure .Net Developer Center](https://azure.microsoft.com/en-us/develop/net/)
If you don't have a Microsoft Azure subscription you can get a FREE trial account [here](http://go.microsoft.com/fwlink/?LinkId=330212)

---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.