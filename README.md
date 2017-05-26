# XRM Update Context
This is a library for updating only those fields that really changed during a transaction in Dynamics CRM / Dynamics365.

# Requirements
This library can be used in CRM Plugins / Workflow Activities and in code of external applications. It is distributed as source file, so you don't need to merge DLLs.
It does not include references / dependencies to any CRM SDK, so you can just install it and choose the CRM SDK that you need yourself.
All CRM versions from 2011 to 365 are supported, just include the one you need to your project.

# Purpose
When sending updates on entities, it is a best practice, to only include those fields in your update object, that really need to be updated.
When using the IOrganizationService, this could look somewhat like this

```C#
var updateRecord = new Entity
{
    Id = oldRecord.Id,
    LogicalName = oldRecord.LogicalName,
    Attributes = new AttributeCollection
    {
      /// Your updates
    }
};

service.Update(updateRecord);
```

This can get messy and is somewhat verbose. You could also use the CrmContext / OrganizationServiceContext if you're programming early-bound, but this library aims to work early- and late-bound, as well as being lightweight.

# NuGet
This library is available as Nuget Package. It is distributed as source file, so you can use it in Plugins and Workflow Activities without having to merge DLLs.

[![NuGet Badge](https://buildstats.info/nuget/Xrm.Oss.UpdateContext.Sources)](https://www.nuget.org/packages/Xrm.Oss.UpdateContext.Sources)

# Usage
## Getting update objects
You can initialize the context to track one of your objects, do any change you like to it, and call the context to get a diff object:

```C#
using(var updateContext = new UpdateContext<Contact>(contact))
{
    contact.FirstName = "Bilbo";
    
    var updateObject = updateContext.GetUpdateObject();
    
    if (updateObject != null)
    {
        service.Update(updateObject);
    }
}
```

## Sending updates automatically
In above code, you need to check the updateObject for null yourself. If you want to send an update directly, you can do the following:

```C#
using(var updateContext = new UpdateContext<Contact>(contact))
{
    contact.FirstName = "Bilbo";
    
    bool updateSent = updateContext.Update(service);
}
```

## Getting update requests
When using Transaction Requests, you might want to get an UpdateRequest for adding it to your transaction:

```C#
using(var updateContext = new UpdateContext<Contact>(contact))
{
    contact.FirstName = "Bilbo";
    
    var updateRequest = updateContext.GetUpdateRequest();
}
```

# How does it work?
Upon initialization, the entity you passed in gets deep copied internally.
On each call to get or send updates, the cloned object is compared to the working object that is still referenced and an update object is built, which only contains the properties that were updated in the working object.

# How to build it
If you want to build this library yourself, just call 

```PowerShell
.\build.cmd
```

# Build Status
[![Build status](https://ci.appveyor.com/api/projects/status/bs2xbv46i34dsw8c?svg=true)](https://ci.appveyor.com/project/DigitalFlow/xrm-update-context)
