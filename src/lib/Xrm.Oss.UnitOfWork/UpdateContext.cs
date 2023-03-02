using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Xrm.Oss.UnitOfWork
{
    public partial class UpdateContext : UpdateContext<Entity>
    {
        public UpdateContext(Entity entity) : base(entity) { }
    }

    public partial class UpdateContext<T> : IDisposable where T : Entity
    {
        private T _initialState;
        private T _workingState;
        private bool _disposed;

        /// <summary>
        /// Clone all attribute types defined in https://msdn.microsoft.com/de-de/library/gg328507(v=crm.6).aspx
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private object CloneAttribute(object value)
        {
            if (value == null)
            {
                return null;
            }

            // Reference Type
            var stringValue = value as string;
            if (stringValue != null)
            {
                return new string(stringValue.ToCharArray());
            }

            // Reference Type
            var optionSetValue = value as OptionSetValue;
            if (optionSetValue != null)
            {
                return new OptionSetValue(optionSetValue.Value);
            }

            // Reference Type
            var entityReferenceValue = value as EntityReference;
            if (entityReferenceValue != null)
            {
                return new EntityReference
                {
                    LogicalName = CloneAttribute(entityReferenceValue.LogicalName) as string,
                    Id = entityReferenceValue.Id,
                    Name = CloneAttribute(entityReferenceValue.Name) as string
                };
            }

            // Reference Type
            var booleanManagedValue = value as BooleanManagedProperty;
            if (booleanManagedValue != null)
            {
                return new BooleanManagedProperty(booleanManagedValue.Value);
            }

            // Reference Type
            var moneyValue = value as Money;
            if (moneyValue != null)
            {
                return new Money(moneyValue.Value);
            }

            // Reference Type
            var aliasedValue = value as AliasedValue;
            if (aliasedValue != null)
            {
                return new AliasedValue(CloneAttribute(aliasedValue.EntityLogicalName) as string, 
                    CloneAttribute(aliasedValue.AttributeLogicalName) as string,
                    CloneAttribute(aliasedValue.Value));
            }

            // Clone all contained records
            var entityCollectionValue = value as EntityCollection;
            if (entityCollectionValue != null)
            {
                return new EntityCollection(entityCollectionValue.Entities.Select(CloneEntity).ToList());
            }

            var optionSetValueCollection = value as OptionSetValueCollection;
            if (optionSetValueCollection != null)
            {
                return new OptionSetValueCollection(optionSetValueCollection.Select(o => new OptionSetValue(o.Value)).ToList());
            }

            var valueTypes = new List<Type>
            {
                typeof(long), typeof(bool), typeof(DateTime),
                typeof(decimal), typeof(double), typeof(int),
                typeof(Guid), typeof(float), typeof(byte), typeof(Enum)
            };

            var type = value.GetType();

            if (valueTypes.Contains(type))
            {
                return value;
            }

            throw new InvalidDataException("Attribute of type '" + type.Name + "' is not supported yet. Please file an issue on GitHub: https://github.com/DigitalFlow/Xrm-Update-Context");
        }

        /// <summary>
        /// Clones entity that is passed in by serializing and deserializing using JSON
        /// </summary>
        /// <param name="entity">Entity to clone</param>
        /// <returns>Deep Copied clone of passed in entity</returns>
        private Entity CloneEntity(Entity entity)
        {
            if (entity == null)
            {
                return null;
            }

            var clone = new Entity
            {
                Id = entity.Id,
                LogicalName = entity.LogicalName
            };

            foreach (var attribute in entity.Attributes)
            {
                clone[attribute.Key] = CloneAttribute(attribute.Value);
            }

            return clone;
        }

        /// <summary>
        /// Creates a new context that tracks changes to the passed in entity
        /// </summary>
        /// <param name="entity">Entity to track changes for</param>
        public UpdateContext(T entity) {
            if (entity == null)
            {
                throw new InvalidOperationException("You must initialize the context using a valid entity, but was null");
            }

            _workingState = entity;
            _initialState = CloneEntity(entity).ToEntity<T>();
        }

        /// <summary>
        /// Returns an update object if tracked entity properties were changed, null otherwise
        /// </summary>
        /// <returns>Update object of same type and with same ID, but only containing attributes that changed since context started tracking.</returns>
        public T GetUpdateObject()
        {
            var update = new Entity
            {
                Id = _initialState.Id,
                LogicalName = _initialState.LogicalName
            };

            foreach (var property in _workingState.Attributes)
            {
                var key = property.Key;
                var value = property.Value;

                // We can't update aliased values, so skip them
                if (value is AliasedValue)
                {
                    continue;
                }

                // Handle newly added attributes
                if (!_initialState.Attributes.ContainsKey(key))
                {
                    // Null valued attributes will not be set in records retrieved using the IOrganizationService
                    // Therefore if attributes are not in the initial state and set null, this should be treated as noop
                    // When a string is cleared on a form, the plugin target will not contain a null value for the attribute, but an empty string. 
                    if (value == null || (value is string && string.IsNullOrEmpty(value as string)))
                    {
                        continue;
                    }
                    
                    update[key] = value;
                }
                // Handle changed attributes
                else if (!object.Equals(_initialState[key], _workingState[key]))
                {
                    update[key] = value;
                }
            }

            if (update.Attributes.Count == 0)
            {
                return null;
            }

            // Return "snapshot" of update object to not include updates that occured on working state after update was generated
            return CloneEntity(update).ToEntity<T>();
        }

        /// <summary>
        /// Sends update with changed attributes only using supplied OrganizationService
        /// </summary>
        /// <param name="service">Organization Service instance to use for sending update</param>
        /// <returns>True if changes found and update send, false otherwise</returns>
        public bool Update(IOrganizationService service)
        {
            var update = GetUpdateObject();

            if (update != null)
            {
                service.Update(update);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns an update request for use in transactions or otherwhere
        /// </summary>
        /// <returns>Update request with target set to update object, or null if nothing changed</returns>
        public UpdateRequest GetUpdateRequest()
        {
            var update = GetUpdateObject();

            if (update != null)
            {
                return new UpdateRequest
                {
                    Target = update
                };
            }

            return null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Free any managed objects here.
                _initialState = null;
            }

            // Free any unmanaged objects here.
            _disposed = true;
        }
    }
}
