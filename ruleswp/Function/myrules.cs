//------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------

namespace rulesnamespace
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Functions.Extensions.Workflows;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.Workflows.RuleEngine;
    using Microsoft.Extensions.Logging;
    using System.Xml;

    /// <summary>
    /// Represents the myrules flow invoked function.
    /// </summary>
    public class myrules
    {
        private readonly ILogger<myrules> logger;

        public myrules(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<myrules>();
        }

        /// <summary>
        /// Executes the logic app workflow.
        /// </summary>
        /// <param name="ruleSetName">The rule set name.</param>
        /// <param name="documentType">document type of input xml.</param>
        /// <param name="inputXml">input xml type fact</param>
        /// <param name="purchaseAmount">purchase amount, value used to create .NET fact </param>
        /// <param name="zipCode">zip code value used to create .NET fact .</param>
        [FunctionName("myrules")]
        public Task<RuleExecutionResult> RunRules(
            [WorkflowActionTrigger] string ruleSetName, 
            string documentType, 
            string inputXml, 
            int purchaseAmount, 
            string zipCode)
        {
            /***** Summary of steps below *****
             * 1. Get the rule set to Execute 
             * 2. Check if the rule set was retrieved successfully
             * 3. create the rule engine object
             * 4. Create TypedXmlDocument facts for all xml document facts
             * 5. Initialize .NET facts
             * 6. Execute rule engine
             * 7. Retrieve relevant updates facts and send them back
             */
            
            try
            {
                // Get the ruleset based on ruleset name
                var ruleExplorer = new FileStoreRuleExplorer();
                var ruleSet = ruleExplorer.GetRuleSet(ruleSetName);

                // Check if ruleset exists
                if(ruleSet == null)
                {
                    // Log an error in finding the rule set
                    this.logger.LogCritical($"RuleSet instance for '{ruleSetName}' was not found(null)");
                    throw new Exception($"RuleSet instance for '{ruleSetName}' was not found.");
                }             

                // Create rule engine instance
                var ruleEngine = new RuleEngine(ruleSet: ruleSet);

                // Create a typedXml Fact(s) from input xml(s)
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(inputXml);
                var typedXmlDocument = new TypedXmlDocument(documentType, doc);

                // Initialize .NET facts
                var currentPurchase = new ContosoNamespace.ContosoPurchase(purchaseAmount, zipCode);

                // Provide facts to rule engine and run it
                ruleEngine.Execute(new object[] { typedXmlDocument, currentPurchase });

                // Send the relevant results(facts) back
                var updatedDoc = typedXmlDocument.Document as XmlDocument;
                var ruleExectionOutput = new RuleExecutionResult()
                {
                    XmlDoc = updatedDoc.OuterXml,
                    PurchaseAmountPostTax = currentPurchase.PurchaseAmount + currentPurchase.GetSalesTax()
                };

                return Task.FromResult(ruleExectionOutput);
            }
            catch(RuleEngineException ruleEngineException)
            {
                // Log any rule engine exceptions
                this.logger.LogCritical(ruleEngineException.ToString());
                throw;
            }
        }

        /// <summary>
        /// Results of the rule execution
        /// </summary>
        public class RuleExecutionResult
        {
            /// <summary>
            /// rules updated xml document
            /// </summary>
            public string XmlDoc { get; set;}

            /// <summary>
            /// Purchase amount post tax
            /// </summary>
            public int PurchaseAmountPostTax { get; set;}
        }
    }
}