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
using Microsoft.Azure.Management.Samples.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

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


        public static void RunSample(ArmClient client)
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
                var webSite_lro = webSiteCollection.CreateOrUpdate(Azure.WaitUntil.Completed, appName, webSiteData);
                var webSite = webSite_lro.Value;

                var functionCollection = webSite.GetSiteFunctions();
                var functionData = new FunctionEnvelopeData()
                {   
                };
                var function_lro = functionCollection.CreateOrUpdate(Azure.WaitUntil.Completed, app1Name, functionData);
                var function = function_lro.Value;

                Utilities.Log("Created function app " + function.Data.Name);
                Utilities.Print(function);

                //============================================================
                // Create a second function app with function level auth

                Utilities.Log("Creating another function app " + app2Name + " in resource group " + rgName + " with function level auth...");
                IAppServicePlan plan = azure.AppServices.AppServicePlans.GetById(app1.AppServicePlanId);
                IFunctionApp app2 = azure.AppServices.FunctionApps.Define(app2Name)
                        .WithExistingAppServicePlan(plan)
                        .WithExistingResourceGroup(rgName)
                        .WithExistingStorageAccount(app1.StorageAccount)
                        .WithLocalGitSourceControl()
                        .Create();

                Utilities.Log("Created function app " + app2.Name);
                Utilities.Print(app2);

                //============================================================
                // Create a third function app with function level auth

                Utilities.Log("Creating another function app " + app3Name + " in resource group " + rgName + " with function level auth...");
                IFunctionApp app3 = azure.AppServices.FunctionApps.Define(app3Name)
                        .WithExistingAppServicePlan(plan)
                        .WithExistingResourceGroup(rgName)
                        .WithExistingStorageAccount(app1.StorageAccount)
                        .WithLocalGitSourceControl()
                        .Create();

                Utilities.Log("Created function app " + app3.Name);
                Utilities.Print(app3);

                //============================================================
                // Deploy to app 1 through Git

                Utilities.Log("Deploying a local function app to " + app1Name + " through Git...");

                IPublishingProfile profile = app1.GetPublishingProfile();
                Utilities.DeployByGit(profile, "square-function-app-admin-auth");

                // warm up
                Utilities.Log("Warming up " + app1Url + "/api/square...");
                Utilities.PostAddress("http://" + app1Url + "/api/square", "625");
                SdkContext.DelayProvider.Delay(5000);
                Utilities.Log("CURLing " + app1Url + "/api/square...");
                Utilities.Log("Square of 625 is " + Utilities.PostAddress("http://" + app1Url + "/api/square?code=" + app1.GetMasterKey(), "625"));

                //============================================================
                // Deploy to app 2 through Git

                Utilities.Log("Deploying a local function app to " + app2Name + " through Git...");

                profile = app2.GetPublishingProfile();
                Utilities.DeployByGit(profile, "square-function-app-function-auth");

                Utilities.Log("Deployment to function app " + app2.Name + " completed");
                Utilities.Print(app2);


                string functionKey = app2.ListFunctionKeys("square").Values.First();

                // warm up
                Utilities.Log("Warming up " + app2Url + "/api/square...");
                Utilities.PostAddress("http://" + app2Url + "/api/square", "725");
                SdkContext.DelayProvider.Delay(5000);
                Utilities.Log("CURLing " + app2Url + "/api/square...");
                Utilities.Log("Square of 725 is " + Utilities.PostAddress("http://" + app2Url + "/api/square?code=" + functionKey, "725"));
            
                Utilities.Log("Adding a new key to function app " + app2.Name + "...");

                var newKey = app2.AddFunctionKey("square", "newkey", null);

                Utilities.Log("CURLing " + app2Url + "/api/square...");
                Utilities.Log("Square of 825 is " + Utilities.PostAddress("http://" + app2Url + "/api/square?code=" + newKey.Value, "825"));

                //============================================================
                // Deploy to app 3 through web deploy

                Utilities.Log("Deploying a local function app to " + app3Name + " throuh web deploy...");

                app3.Deploy()
                    .WithPackageUri("https://github.com/Azure/azure-libraries-for-net/raw/master/Samples/Asset/square-function-app-function-auth.zip")
                    .WithExistingDeploymentsDeleted(false)
                    .Execute();

                Utilities.Log("Deployment to function app " + app3.Name + " completed");

                Utilities.Log("Adding a new key to function app " + app3.Name + "...");
                app3.AddFunctionKey("square", "newkey", "mysecretkey");

                // warm up
                Utilities.Log("Warming up " + app3Url + "/api/square...");
                Utilities.PostAddress("http://" + app3Url + "/api/square", "925");
                SdkContext.DelayProvider.Delay(5000);
                Utilities.Log("CURLing " + app3Url + "/api/square...");
                Utilities.Log("Square of 925 is " + Utilities.PostAddress("http://" + app3Url + "/api/square?code=mysecretkey", "925"));
            }
            finally
            {
                try
                {
                    Utilities.Log("Deleting Resource Group: " + rgName);
                    azure.ResourceGroups.DeleteByName(rgName);
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

        public static void Main(string[] args)
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

                RunSample(client);
            }
            catch (Exception e)
            {
                Utilities.Log(e);
            }
        }
    }
}