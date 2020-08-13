using System;
using System.Configuration;
using System.Linq;
using Microsoft.PowerPlatform.Cds.Client;
using Microsoft.Xrm.Sdk;

namespace CDS.LookupError
{
    class Program
    {
        static void Main(string[] args)
        {
            var svc = new CdsServiceClient(ConfigurationManager.AppSettings["ConnectionString"]);

            if (!svc.IsReady)
                return;

            PolyLookup(svc);
            CustomLookup(svc);
        }

        private static void PolyLookup(CdsServiceClient svc)
        {
            var account = new Account
            {
                Name = "Super Awesome"
            };
            var accountId = svc.Create(account);

            var contact = new Contact
            {
                FirstName = "Super",
                LastName = "Awesome",
                ParentCustomerId = new EntityReference(Account.EntityLogicalName, accountId)
            };

            svc.Create(contact);

        }
        
        private static void CustomLookup(CdsServiceClient svc)
        {

            var cdsServiceContext = new CdsServiceContext(svc);

            var vicId = (from s in cdsServiceContext.CreateQuery<new_state>()
                         where s.new_name == "Victoria"
                         select s.Id).FirstOrDefault();

            if (vicId == Guid.Empty)
            {
                var stateVic = new new_state
                {
                    new_name = "Victoria"
                };

                vicId = svc.Create(stateVic);
            }

            var contact = new Contact
            {
                FirstName = "Test",
                LastName = "Address 1 State Entity Ref",
                new_Address1StateId = new EntityReference(new_state.EntityLogicalName, vicId) //Standard case on Schema Name when Creating
            };

            CreateContactRecord(contact, svc);

            contact = new Contact
            {
                FirstName = "Test",
                LastName = "Address 2 State Entity Ref",
                new_address2stateid = new EntityReference(new_state.EntityLogicalName, vicId) //Forced Lowercase on Schema Name when Creating
            };
            CreateContactRecord(contact, svc); //Works

            contact = new Contact
            {
                FirstName = "Test",
                LastName = "Address 1 Crafted Name without bind, with case",
            };
            contact["new_Address1StateId"] = new EntityReference(new_state.EntityLogicalName, vicId);
            CreateContactRecord(contact, svc); //Doesn't WOrk

            contact = new Contact
            {
                FirstName = "Test",
                LastName = "Address 1 Crafted Name with case & Bind",
            };
            contact["new_Address1StateId@odata.bind"] = new EntityReference(new_state.EntityLogicalName, vicId);
            CreateContactRecord(contact, svc); //Works

            contact = new Contact
            {
                FirstName = "Test",
                LastName = "Address 1 Crafted Name without case & Bind",
            };
            contact["new_address1stateid"] = new EntityReference(new_state.EntityLogicalName, vicId);
            CreateContactRecord(contact, svc); //Doesn't Work

            contact = new Contact
            {
                FirstName = "Test",
                LastName = "Address 1 Crafted Name with bind, without case",
            };
            contact["new_address1stateid@odata.bind"] = new EntityReference(new_state.EntityLogicalName, vicId);
            CreateContactRecord(contact, svc); //Doesn't Work
        }

        private static void CreateContactRecord(Contact contact, CdsServiceClient svc)
        {
            Console.WriteLine("\n======================================\n");
            try
            {
                var id = svc.Create(contact);
                Console.WriteLine($"Created Contact {contact.LastName} with ID: {id}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to create contact {contact.LastName}");
                Console.WriteLine(e);
            }
        }
    }
}
