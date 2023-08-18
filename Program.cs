// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Samples.Common;
using Azure;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Azure.ResourceManager.AppService.Models;

namespace ManageFunctionAppWithAuthentication
{
    public class Program
    {
        /**
         * Azure App Service basic sample for managing function apps.
         *  - Create 3 function apps under the same new app service plan and with the same storage account
         *    - Deploy 1 and 2 via Git a function that calculates the square of a number
         *    - Deploy 3 via Web Deploy
         *    - Enable app level authentication for the 1st function app
         *    - Verify the 1st function app can be accessed with the admin key
         *    - Enable function level authentication for the 2nd function app
         *    - Verify the 2nd function app can be accessed with the function key
         *    - Enable function level authentication for the 3rd function app
         *    - Verify the 3rd function app can be accessed with the function key
         */


        public static async Task RunSample(ArmClient client)
        {
            // New resources
            AzureLocation region = AzureLocation.EastUS;
            string suffix         = ".azurewebsites.net";
            string appName        = Utilities.CreateRandomName("webapp-");
            string app1Name       = Utilities.CreateRandomName("webapp1-");
            string app2Name       = Utilities.CreateRandomName("webapp2-");
            string app3Name       = Utilities.CreateRandomName("webapp3-");
            string app1Url        = app1Name + suffix;
            string app2Url        = app2Name + suffix;
            string app3Url        = app3Name + suffix;
            string rgName         = Utilities.CreateRandomName("rg1NEMV_");
            var lro = client.GetDefaultSubscription().GetResourceGroups().CreateOrUpdate(Azure.WaitUntil.Completed, rgName, new ResourceGroupData(AzureLocation.EastUS));
            var resourceGroup = lro.Value;

            try {


                //============================================================
                // Create a function app with admin level auth

                Utilities.Log("Creating function app " + app1Name + " in resource group " + rgName + " with admin level auth...");

                var webSiteCollection = resourceGroup.GetWebSites();
                var webSiteData = new WebSiteData(region)
                {
                    SiteConfig = new Azure.ResourceManager.AppService.Models.SiteConfigProperties()
                    {
                        WindowsFxVersion = "PricingTier.StandardS1",
                        NetFrameworkVersion = "NetFrameworkVersion.V4_6",
                    }
                };
                var webSite_lro =await  webSiteCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, appName, webSiteData);
                var webSite = webSite_lro.Value;

                var functionCollection = webSite.GetSiteFunctions();
                var functionData = new FunctionEnvelopeData()
                {   
                };
                var function_lro =await functionCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, app1Name, functionData);
                var function = function_lro.Value;

                Utilities.Log("Created function app " + function.Data.Name);
                Utilities.Print(function);

                //============================================================
                // Create a second function app with function level auth

                Utilities.Log("Creating another function app " + app2Name + " in resource group " + rgName + " with function level auth...");
                var plan = webSite.Data.AppServicePlanId;
                var function2Collection = webSite.GetSiteFunctions();
                var function2Data = new FunctionEnvelopeData()
                {
                };
                var function2_lro = await functionCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, app1Name, functionData);
                var function2 = function_lro.Value;

                Utilities.Log("Created function app " + function2.Data.Name);
                Utilities.Print(function2);

                //============================================================
                // Create a third function app with function level auth

                Utilities.Log("Creating another function app " + app3Name + " in resource group " + rgName + " with function level auth...");
                var function3Collection = webSite.GetSiteFunctions();
                var function3Data = new FunctionEnvelopeData()
                {
                };
                var function3_lro = await functionCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, app1Name, functionData);
                var function3 = function_lro.Value;

                Utilities.Log("Created function app " + function3.Data.Name);
                Utilities.Print(function3);

                //============================================================
                // Deploy to app 1 through Git

                Utilities.Log("Deploying a local function app to " + app1Name + " through Git...");

                var profile_lro = await webSite.GetPublishingProfileXmlWithSecretsAsync(new CsmPublishingProfile()
                {
                    Format = PublishingProfileFormat.Ftp
                });
                var profile = profile_lro.Value;
                Utilities.DeployByGit(profile, "square-function-app-admin-auth");

                // warm up
                Utilities.Log("Warming up " + app1Url + "/api/square...");
                Utilities.PostAddress("http://" + app1Url + "/api/square", "625");
                Thread.Sleep(5000);
                Utilities.Log("CURLing " + app1Url + "/api/square...");
                Utilities.Log("Square of 625 is " + Utilities.PostAddress("http://" + app1Url + "/api/square?code=" + function.GetFunctionKeys(), "625"));

                //============================================================
                // Deploy to app 2 through Git

                Utilities.Log("Deploying a local function app to " + app2Name + " through Git...");

                Utilities.DeployByGit(profile, "square-function-app-function-auth");

                Utilities.Log("Deployment to function app " + function2.Data.Name + " completed");
                Utilities.Print(function2);


                var functionKey_lro =await function2.GetFunctionKeysAsync();
                var functionKey = functionKey_lro.Value;
                var functionKey1 = functionKey.Properties.First();

                // warm up
                Utilities.Log("Warming up " + app2Url + "/api/square...");
                Utilities.PostAddress("http://" + app2Url + "/api/square", "725");
                Thread.Sleep(5000);
                Utilities.Log("CURLing " + app2Url + "/api/square...");
                Utilities.Log("Square of 725 is " + Utilities.PostAddress("http://" + app2Url + "/api/square?code=" + functionKey1, "725"));
            
                Utilities.Log("Adding a new key to function app " + function2.Data.Name + "...");

                var newKey = function2.CreateOrUpdateFunctionSecret("square", new WebAppKeyInfo()
                {
                    Properties = new WebAppKeyInfoProperties()
                    {
                        Name = "newkey",
                    }
                });

                Utilities.Log("CURLing " + app2Url + "/api/square...");
                Utilities.Log("Square of 825 is " + Utilities.PostAddress("http://" + app2Url + "/api/square?code=" + newKey.Value, "825"));

                //============================================================
                // Deploy to app 3 through web deploy

                Utilities.Log("Deploying a local function app to " + app3Name + " throuh web deploy...");

                var extensionResource = webSite.GetSiteExtension();
                var deployment = await extensionResource.CreateOrUpdateAsync(WaitUntil.Completed, new WebAppMSDeploy()
                {
                    PackageUri = new Uri("https://github.com/Azure/azure-libraries-for-net/raw/master/Samples/Asset/square-function-app-function-auth.zip"),
                });

                Utilities.Log("Deployment to function app " + function3.Data.Name + " completed");

                Utilities.Log("Adding a new key to function app " + function.Data.Name + "...");
                function3.CreateOrUpdateFunctionSecret("square",new WebAppKeyInfo()
                {
                    Properties = new WebAppKeyInfoProperties()
                    {
                        Name = "newkey",
                        Value = "mysecretkey"
                    }
                });

                // warm up
                Utilities.Log("Warming up " + app3Url + "/api/square...");
                Utilities.PostAddress("http://" + app3Url + "/api/square", "925");
                Thread.Sleep(5000);
                Utilities.Log("CURLing " + app3Url + "/api/square...");
                Utilities.Log("Square of 925 is " + Utilities.PostAddress("http://" + app3Url + "/api/square?code=mysecretkey", "925"));
            }
            finally
            {
                try
                {
                    Utilities.Log("Deleting Resource Group: " + rgName);
                    await resourceGroup.DeleteAsync(WaitUntil.Completed);
                    Utilities.Log("Deleted Resource Group: " + rgName);
                }
                catch (NullReferenceException)
                {
                    Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
                }
                catch (Exception g)
                {
                    Utilities.Log(g);
                }
            }
        }

        public static async Task Main(string[] args)
        {
            try
            {
                //=================================================================
                // Authenticate
                var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
                var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
                var subscription = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");
                ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                ArmClient client = new ArmClient(credential, subscription);

                // Print selected subscription
                Utilities.Log("Selected subscription: " + client.GetSubscriptions().Id);

                await RunSample(client);
            }
            catch (Exception e)
            {
                Utilities.Log(e);
            }
        }
    }
}