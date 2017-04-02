using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace Xrm.Oss.UnitOfWork
{
    public partial class UpdateContext<T> : IDisposable where T : Entity
    {
        private T _initialState;
        private T _workingState;
        private bool _disposed;

        /// <summary>
        /// Clones entity that is passed in by serializing and deserializing using JSON
        /// </summary>
        /// <param name="entity">Entity to clone</param>
        /// <returns>Deep Copied clone of passed in entity</returns>
        private T CloneEntity(T entity)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));

            using (var memoryStream = new MemoryStream())
            {
                serializer.WriteObject(memoryStream, entity);

                // Reset stream position before reading again
                memoryStream.Position = 0;

                return (T) serializer.ReadObject(memoryStream);
            }
        }

        /// <summary>
        /// Creates a new context that tracks changes to the passed in entity
        /// </summary>
        /// <param name="entity">Entity to track changes for</param>
        public UpdateContext(T entity) {
            _workingState = entity;
            _initialState = CloneEntity(entity);
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

                // Handle newly added attributes
                if (!_initialState.Attributes.ContainsKey(key))
                {
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

            return update.ToEntity<T>();
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
