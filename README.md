# PeliQ

Modder framework, adds some item query related utilities.

See `[CP] PeliQ Examples` for samples.


### Nested Item Queries

Run 1 item query, then pass it's results to another item query for further processing.
The inner item query goes in `ModData`.

For example, this item query, if placed under a shop Items field, makes that shop sell every kind of fish bait including any modded fish.

```json
{
  "Id": "{{ModId}}_AllBait_Outer",
  "ItemId": "ALL_ITEMS (O)",
  "PerItemCondition": "ITEM_CONTEXT_TAG Target category_fish",
  "ModData": {
    "Id": "{{ModId}}_AllBait_Inner",
    "ItemId": "FLAVORED_ITEM Bait mushymato.PeliQ_NestedId",
  }
}
```

The inner item query:

- It is required to have `mushymato.PeliQ_NestedId` as part of `ItemId`, i.e. cannot use non-ItemId queries here
- Alteratively, you can put `mushymato.PeliQ_NestedId` with `InputId`.
- can only nest once
- At the moment, this inner query cannot accept certain types, namely lists.

The specific flavor of item spawn data used is `PeliQSpawnItemData` which extends [MachineItemOutput](https://stardewvalleywiki.com/Modding:Machines) with 1 more field `InputId` that acts as the machine input.
You can spawn custom flavored items like so:

```json
"ModData": [
  {
    "Id": "{{ModId}}_CustomPreserve",
    "ItemId": "(O)107", // dino egg
    "ObjectDisplayName": "[LocalizedText Strings\\Objects:SmokedFish_Description %PRESERVED_DISPLAY_NAME]",
    "ObjectInternalName": "DINO EGG TEST",
    "PreserveId": "DROP_IN",
    "CopyColor": true,
    "InputId": "(O)Moss"
  }
]
```

The outer query's results are passed into the inner as `InputId` if `InputId=mushymato.PeliQ_NestedId`.

### Stored Item Queries `mushymato.PeliQ/ItemQueries`

Add a new asset for item queries that can be used by key in various places.
The custom asset is a list of item queries with condition.
This also uses the mentioned `PeliQSpawnItemData` type.

```json
{
  "Action": "EditData",
  "Target": "mushymato.PeliQ/ItemQueries",
  "Entries": {
    "{{ModId}}_Jelly": [
      {
        "Id": "{{ModId}}_Jelly1",
        "ItemId": "(O)CaveJelly",
      },
      {
        "Id": "{{ModId}}_Jelly2",
        "ItemId": "(O)RiverJelly",
      },
      {
        "Id": "{{ModId}}_Jelly3",
        "ItemId": "(O)SeaJelly",
      }
    ],
    "{{ModId}}_Fish": [
      {
        "Id": "{{ModId}}_Fish",
        "ItemId": "ALL_ITEMS (O)",
        "PerItemCondition": "ITEM_CONTEXT_TAG Target category_fish",
      }
    ]
  }
},
```

These can be used in:

- Item queries: `mushymato.PeliQ_STORED_QUERY <queryId>` in the ItemId of another ItemQuery
- Trigger action: `mushymato.PeliQ_AddItemByQuery <queryId> [ItemQuerySearchMode]`
- Tile Action and TouchAction: `mushymato.PeliQ_AddItemByQuery <queryId> [ItemQuerySearchMode] [isDebris]`
- Mail: `%peliQ <queryId> [ItemQuerySearchMode] %%` in mail, the items will appear on mail and need to be removed one by one.

##### ItemQuerySearchMode

One of:

- `All`
- `AllOfTypeItem`
- `FirstOfTypeItem`
- `RandomOfTypeItem`

The default is `AllOfTypeItem`, note that these are applied to every query in the list, so if you have 3 queries that each give 1 specific item (like the `{{ModId}}_Jelly` example) and use `RandomOfTypeItem`, you will get 3 jellies because 3 item queries have ran. Use fields like `RandomItemId` to randomize one item query.

##### isDebris

The trigger action always add item directly to player inventory, while the 2 map actions will spawn debris unless isDebris is `false`.

### New Item Queries

#### mushymato.PeliQ_ACTION_SALABLE \<itemId\>

- Adds a bogus item entry in the shop that doesn't actually buy items.
- This is intended to be used for the purpose of creating a shop entry that activates `ActionsOnPurchase` without giving you an actual item.
- `itemId` is not qualified and must be an `Data/Objects` entry.

There are 2 custom fields that can be set on the `Data/Objects` entry of your placeholder object.

- `mushymato.PeliQ/ExitOnPurchase`: Exit the menu immediately on purchase
- `mushymato.PeliQ/PurchaseSound`: Sound to play on purchase

### New Game State Queries

#### mushymato.PeliQ_ITEM_ID_REGEX Target|Input \<regex\>+

- Matches item id or qualified item id against a [regular expression (case insensitive)](https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference).
- First argument is usually Target, but Input if dealing with machine trigger rules.

### mushymato.PeliQ_ANY_N "first GSQ" \<count\> \<"GSQ"\>+

- Evaluate first GSQ, then check that at least `count` number of the following `GSQ`'s results equals the first GSQ's result.
- For basic usage you can use `TRUE` or `FALSE` as the first GSQ.
- If the GSQ has spaces, it must be quote escaped.

### mushymato.PeliQ_AND "first GSQ" "second GSQ"

- Evaluate 2 GSQ, and check for AND.
- Added just for fun, you can already use `GSQ,GSQ` for AND, and `ANY "!GSQ" "!GSQ"` for !AND.

### mushymato.PeliQ_OR "first GSQ" "second GSQ"

- Evaluate 2 GSQ, and check for AND.
- Added just for fun, you can already use `ANY GSQ GSQ` for OR.

### mushymato.PeliQ_XOR "first GSQ" "second GSQ"

- Evaluate 2 GSQ, and check for XOR (aka first does not equal second).
- Added just for fun, you can already use `ANY "GSQ,!GSQ" "!GSQ,GSQ"` for XOR.
