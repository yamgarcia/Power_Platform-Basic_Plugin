using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlugin
{
    public class ValidateAccountName : IPlugin
    {
        //Invalid names from unsecure configuration
        private List<string> invalidNames = new List<string>();

        // Constructor to capture the unsecure configuration
        public ValidateAccountName(string unsecure)
        {
            // Parse the configuration data and set invalidNames
            if (!string.IsNullOrWhiteSpace(unsecure))
                unsecure.Split(',').ToList().ForEach(s =>
                {
                    invalidNames.Add(s.Trim());
                });
        }
        public void Execute(IServiceProvider serviceProvider)
        {

            // Obtain the tracing service
            ITracingService tracingService =
            (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            try
            {

                // Obtain the execution context from the service provider.  
                IPluginExecutionContext context = (IPluginExecutionContext)
                    serviceProvider.GetService(typeof(IPluginExecutionContext));

                // Verify all the requirements for the step registration
                if (context.InputParameters.Contains("Target") && //Is a message with Target
                    context.InputParameters["Target"] is Entity && //Target is an entity
                    ((Entity)context.InputParameters["Target"]).LogicalName.Equals("account") && //Target is an account
                    ((Entity)context.InputParameters["Target"])["name"] != null && //account name is passed
                    context.MessageName.Equals("Update") && //Message is Update
                    context.PreEntityImages["a"] != null && //PreEntityImage with alias 'a' included with step
                    context.PreEntityImages["a"]["name"] != null) //account name included with PreEntityImage with step
                {
                    // Obtain the target entity from the input parameters.  
                    var entity = (Entity)context.InputParameters["Target"];
                    var newAccountName = (string)entity["name"];
                    var oldAccountName = (string)context.PreEntityImages["a"]["name"];

                    if (invalidNames.Count > 0)
                    {
                        tracingService.Trace("ValidateAccountName: Testing for {0} invalid names:", invalidNames.Count);

                        if (invalidNames.Contains(newAccountName.ToLower().Trim()))
                        {
                            tracingService.Trace("ValidateAccountName: new name '{0}' found in invalid names.", newAccountName);

                            // Test whether the old name contained the new name
                            if (!oldAccountName.ToLower().Contains(newAccountName.ToLower().Trim()))
                            {
                                tracingService.Trace("ValidateAccountName: new name '{0}' not found in '{1}'.", newAccountName, oldAccountName);

                                string message = string.Format("You can't change the name of this account from '{0}' to '{1}'.", oldAccountName, newAccountName);

                                throw new InvalidPluginExecutionException(message);
                            }

                            tracingService.Trace("ValidateAccountName: new name '{0}' found in old name '{1}'.", newAccountName, oldAccountName);
                        }

                        tracingService.Trace("ValidateAccountName: new name '{0}' not found in invalidNames.", newAccountName);
                    }
                    else
                    {
                        tracingService.Trace("ValidateAccountName: No invalid names passed in configuration.");
                    }
                }
                else
                {
                    tracingService.Trace("ValidateAccountName: The step for this plug-in is not configured correctly.");
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace("BasicPlugin: {0}", ex.ToString());
                throw;
            }
        }
    }
}
