using FakeItEasy;
using FakeXrmEasy;
using Microsoft.Xrm.Sdk;
using System;
using Xunit;

namespace Xrm.Oss.UnitOfWork.Tests
{
    public class UpdateContextTests
    {
        [Fact]
        public void Should_Keep_Logical_Name_And_Id()
        {
            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid()
            };

            using (var updateContext = new UpdateContext<Entity>(contact))
            {
                contact["firstname"] = "Frodo";

                var update = updateContext.GetUpdateObject();

                Assert.Equal(contact.LogicalName, update.LogicalName);
                Assert.Equal(contact.Id, update.Id);
            }
        }

        [Fact]
        public void Should_Add_Newly_Added_Attributes()
        {
            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid()
            };

            using (var updateContext = new UpdateContext<Entity>(contact))
            {
                contact["firstname"] = "Frodo";

                var update = updateContext.GetUpdateObject();

                Assert.Equal("Frodo", update["firstname"]);
            }
        }

        [Fact]
        public void Should_Not_Add_Unchanged_Attributes()
        {
            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "lastname", "Baggins" }
                }
            };
            
            using (var updateContext = new UpdateContext<Entity>(contact))
            {
                contact["firstname"] = "Frodo";

                var update = updateContext.GetUpdateObject();

                Assert.Equal("Frodo", update["firstname"]);
                Assert.False(update.Contains("lastname"));
            }
        }

        [Fact]
        public void Should_Return_Null_If_Nothing_Changed()
        {
            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid()
            };

            using (var updateContext = new UpdateContext<Entity>(contact))
            {
                var update = updateContext.GetUpdateObject();

                Assert.Null(update);
            }
        }

        [Fact]
        public void Should_Include_Attributes_That_Were_Set_Null()
        {
            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "lastname", "Baggins" }
                }
            };

            using (var updateContext = new UpdateContext<Entity>(contact))
            {
                contact["lastname"] = null;

                var update = updateContext.GetUpdateObject();

                Assert.Null(update["lastname"]);
            }
        }

        [Fact]
        public void Should_Return_UpdateRequest_With_Proper_Target()
        {
            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid()
            };

            using (var updateContext = new UpdateContext<Entity>(contact))
            {
                contact["firstname"] = "Frodo";

                var updateRequest = updateContext.GetUpdateRequest();

                Assert.Equal("Frodo", updateRequest.Target["firstname"]);
            }
        }

        [Fact]
        public void Should_Send_Update_If_Changes_Made()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid()
            };

            context.Initialize(new[]{ contact });
            
            using (var updateContext = new UpdateContext<Entity>(contact))
            {
                contact["firstname"] = "Frodo";

                var updateSent = updateContext.Update(service);

                Assert.True(updateSent);
                A.CallTo(() => service.Update(A<Entity>._)).MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [Fact]
        public void Should_Not_Send_Update_If_No_Changes_Made()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            
            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid()
            };
            
            context.Initialize(new[]{ contact });

            using (var updateContext = new UpdateContext<Entity>(contact))
            {
                var updateSent = updateContext.Update(service);

                Assert.False(updateSent);
                A.CallTo(() => service.Update(A<Entity>._)).MustNotHaveHappened();
            }
        }
    }
}
