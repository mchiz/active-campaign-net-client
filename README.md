# ActiveCampaign .Net Client (C#)
The purpose of ActiveCampaign .Net Client is to interact with ActiveCampaign API V3 without having to know the in and outs of it.

It is thread-safe and you can use multiple clients at the same time. But beware! ActiveCampaign does not allow to do more than 5 requests per second. This library takes that into account, and if you use multiple clients/threads it will still respect this time access limitation and function properly, so your program might not run as fast as you may expect due to this.

## How to use

### Initialization

```csharp
using var ac = new ActiveCampaign.Client( "<API access url here>", "<API access key here>" );
```
You can find your API access url and key in your ActiveCampaign account: Settings (cog) -> Developer

### Adding a contact

```csharp
var contactData = await ac.AddContact( "example@example.com", <optional cancellation token> );
```
