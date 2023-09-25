# NostrSharp
## A library that provide a set of tools for Nostr developement

## Preview
This library is inspired by [this repo](https://github.com/Marfusios/nostr-client).
You can freely use NostrSharp in any kind of projects you want.

> **IMPORTANT!**
> **This library is not yet tested enough for a production environment. Feel free to try it, send feedback and contribute.**

Download the package from [Nuget](https://www.nuget.org/packages/NostrSharp) or search it by typing NostrSharp.

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


## WEB ENVIRONMENT
Because of .NET 6 current lack of support for all encryption primitives, i came up with the possibility to pass in custom encryption/decryption methods to extend this support in framework like *Blazor*.

For example if you check the *EncryptBase64* method inside *NEvent* class, you can see how it uses *System.Security.Cryptography.Aes*: this has no support at all on .NET 6 and a very limited support on .NET 7, so this will throw PlatformNotSupported exceptions respectively on '*Aes.Create()*' for .net 6 and '*aes.EncryptCbc*' on .net 7, making impossible to use NIP-04 on Blazor
```csharp
// Not supported on .NET 6
using Aes aes = Aes.Create();
// Not supported on .NET 6 and .NET 7
byte[] cbcEncrypted = aes.EncryptCbc(plainText, aes.IV);
```

**HOWEVER HERE'S THE SOLUTION I FOUND**

Add this on you Blazor's project *wwwroot/index.html* file somewhere inside the *body* tag
```csharp
<script>
    window.ArrayBufferToBase64 = function (arrayBufferText) {
        var binary = '';
        var bytes = new Uint8Array(arrayBufferText);
        var len = bytes.byteLength;
        for (var i = 0; i < len; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return window.btoa(binary);
    };
    window.ImportAesCbcKey = async function (sharedKeyAsByteArray) {
        var cryptoKey = await crypto.subtle.importKey(
            "raw", // format
            sharedKeyAsByteArray, // keyData
            { name: "AES-CBC" }, // algorithm
            true, // extractable
            ["encrypt", "decrypt"], // keyUsages
        );
        console.log('cryptoKey: ' + cryptoKey);
        return cryptoKey;
    };


    /// <summary>
    /// Takes the key and the plain text to encrypt using AES-256-CBC algorithm.
    /// </summary>
    /// <param name="sharedKeyAsByteArray">Byte array representation of key's EC</param>
    /// <param name="plainTextAsByteArray">UTF8 plain text as byte array</param>
    /// <returns>The encrypted string formatted as => encrypted_text + '?iv=' + init_vector</returns>
    window.AesCbcEncrypt = async function (sharedKeyAsByteArray, plainTextAsByteArray) {

        // Import the key as an AES-256-CBC key and generate a init vector
        var cryptoKey = await window.ImportAesCbcKey(sharedKeyAsByteArray);
        const iv = crypto.getRandomValues(new Uint8Array(16));

        console.log('iv: ' + iv);

        // Run the encryption
        // reference: https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/encrypt
        var encryptedText = await crypto.subtle.encrypt(
            { name: "AES-CBC", iv },
            cryptoKey,
            plainTextAsByteArray
        );
        console.log('encryptedText: ' + encryptedText);

        var base64encodedText = window.ArrayBufferToBase64(encryptedText);
        var base64encodedIV = window.ArrayBufferToBase64(iv);

        console.log('RESULT: ' + base64encodedText + "?iv=" + base64encodedIV);

        // And finally the result returned is always formatted with iv separator ("?iv=")
        return base64encodedText + "?iv=" + base64encodedIV;
    };

    /// <summary>
    /// Takes the key, the init vector and the encrypted text decrypt using AES-256-CBC algorithm.
    /// </summary>
    /// <param name="sharedKeyAsByteArray">Byte array representation of key's EC</param>
    /// <param name="ivAsByteArray">Byte array representation init vector</param>
    /// <param name="encryptedTextAsByteArray">UTF8 encrypted text as byte array</param>
    /// <returns>The encrypted string formatted as => encrypted_text + '?iv=' + init_vector</returns>
    window.AesCbcDecrypt = async function (sharedKeyAsByteArray, ivAsByteArray, encryptedTextAsByteArray) {

        // Import the key as an AES-256-CBC
        var cryptoKey = await window.ImportAesCbcKey(sharedKeyAsByteArray);

        // Run the decryption
        // reference: https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/decrypt
        var decryptedText = await crypto.subtle.decrypt(
            { name: "AES-CBC", iv: ivAsByteArray.buffer }, // Init vector must be passed from outside
            cryptoKey,
            encryptedTextAsByteArray
        );

        // Convert the decrypted text from ArrayBuffer to Uint8Array, and then decode it as string
        return new TextDecoder('utf-8').decode(new Uint8Array(decryptedText));
    };
</script>
```

Then create a new class with this code
```csharp
public class BlazorAesCbc
{
    [Inject]
    private IJSRuntime JS { get; set; }

    public BlazorAesCbc(IJSRuntime js)
    {
        this.JS = js;
    }


    public async Task<string?> Encrypt(byte[] sharedKeyAsByteArray, byte[] plainTextAsByteArray)
    {
        try
        {
            string result = await JS.InvokeAsync<string>("AesCbcEncrypt", sharedKeyAsByteArray, plainTextAsByteArray);
            return result;
        }
        catch (Exception ex)
        {
            return null;
        }
    }
    public async Task<string?> Decrypt(byte[] sharedKeyAsByteArray, byte[] ivAsByteArray, byte[] encryptedTextAsByteArray)
    {
        try
        {
            string result = await JS.InvokeAsync<string>("AesCbcDecrypt", sharedKeyAsByteArray, ivAsByteArray, encryptedTextAsByteArray);
            return result;
        }
        catch (Exception ex)
        {
            return null;
        }
    }
}
```
And, for the last step, add this on you *Program.cs* file, right before calling *var host = builder.Build();*
```csharp
builder.Services.AddSingleton<BlazorAesCbc>();
```

Now, whenever you need to encrypt/decrypt an event's Content, you can import the *BlazorAesCbc* class inside your page and pass his methods to encrypt/decrypt as shown here:
```csharp
// This at the top of the page
@inject BlazorAesCbc bai

// This where you need to encrypt
NEvent? ev = await NSEventMaker.EncryptedDirectMessage("test di criptazione", ns.NPub, ns.NSec, bai.Encrypt);

// This where you need to decrypt
string? decryptedMsg = await ev.Decrypt(ns.NSec, bai.Decrypt);
```
