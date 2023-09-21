# NostrSharp
## A library that provide a set of tools for Nostr developement

## Preview
This library is inspired by [this repo](https://github.com/Marfusios/nostr-client).
You can freely use NostrSharp in any kind of projects you want.

> **IMPORTANT!**
> **This library is not yet tested enough for a production environment. Feel free to try it, send feedback and contribute.**


This library is meant to provide necessary tools to build your own nostr-based software
It's usable both on desktop/mobile and web environments.

I've implemented all kinds of events detailed in the Nostr repository, **HOWEVER** i didn't yet tested most of the implementations i made, so be careful.

Some of the things i tested:
- multiple relays connection
- obtaining relays NIP-11 information document
- requesting events with filters
- NPub and NSec generation
- keys conversions between hex and bech32
- events signing
- events serialization/deserialization
- publishing events to multiple relays


## How to use it
The easiest way in my opinion is to rely entirely on the **NSMain** class, however, since the vast majority of the library's classes and methods are public you are not forced to.
```csharp
NSMain ns = new NSMain();

// Initialize it by giving at least one of NPub or NSec
ns.Init(nPub, nSec);

// Connect to a list of relays by they uris
await ns.ConnectRelays(relaysUriList);
// Add a new relay in any moment
ns.AddRelay(relayUri);
// Remove a relay in any moment
await ns.DisconnectRelay(relayUri);

// Asking to all connected relays all my Kind 1 events (uses previously given NPub)
await ns.GetMyShortTextNotes();

// Don't forget to dispose
ns.Dispose();
```

When you want to create and fill a new event you could use the static class **NSEventMaker**.
This class contains a comprehensive collection of helper methods to help you generate the *NEvent* you want.

For example, to create a ready-to-sign event for a short text note you could write the following:
```csharp
NEvent ev = NSEventMaker.ShortTextNote("Hello World! This is my first note!");
```

The resulting *NEvent* is the instance you need to sign and then broadcast to relays:
```csharp
// If the signing procedure goes well
if (ev.Sign(nSec))
{
    // I can broadcast the event to all my connected relays
    await ns.SendEvent(ev);

    // Or maybe to just one
    await ns.SendEvent(specificRelayUri, ev);
}
```

Whenever i'm requesting a filtered set of events, or broadcasting a new one, i will always need to listen for result events:
```csharp
// If i want to listen for all Kind 1 events i receive from relays i have to subscribe to this event
ns.OnShortTextNote += OnShortTextNote;

void OnShortTextNote(Uri relayUri, NEvent receivedShortTextNoteEvent)
{
    // I receive the uri of the relay that has sent the event, and the deserialized event itself
}

// If i want to know if an event i broadcasted is approved or refused
ns.OnEventApproved += OnEventApproved;
ns.OnEventRefused += OnEventRefused;

void OnEventApproved(Uri relayUri, string eventId, string message)
{
    // I receive the uri of the responding relay, the id of the event i tried to broadcast
    // and an optional error message from the relay describing the operation
}
void OnEventRefused(Uri relayUri, string eventId, string message)
{
    // I receive the uri of the responding relay, the id of the event i tried to broadcast
    // and an optional text message from the relay describing the operation
}
```

## Other Stuff

### Keys
You can generate a new key pair
```csharp
NSKeyPair kp = NSKeyPair.GenerateNew();
```

You can import an existing key from both Hex or Bech32 formats, and then have it on the other format
```csharp
NSec nsec1 = NSec.FromBech32("bech32_nsec_string");
string hex = nsec1.Hex;

NSec nsec2 = NSec.FromHex("hex_nsec_string");
string bech32 = nsec2.Bech32;
```


### Utilities
You can verify NIP05:
```csharp
NIP05? checkedNIP05 = await NSUtilities.CheckNIP05(nPubHexToCheck, nip05String);
if (checkedNIP05 is not null)
{
    // Valid NIP-05
}
```

When you need to parse the event content to get all embedded references to events or profiles, you can use *EventContentParser*
```csharp
// This return a list of all the identifiers found inside the event's content
List<NostrIdentifier> identifiers = EventContentParser.ParseEventContent(eventToBeParsed);
```



