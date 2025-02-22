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

- is required to have `mushymato.PeliQ_NestedId` as part of `ItemId`, i.e. cannot use non-ItemId queries here
- can have the generic item query fields, as well as `Condition` GSQ
- can only nest once

### Stored Item Queries `mushymato.PeliQ/ItemQueries`

Add a new asset for item queries that can be used by key in various places.
The custom asset is a list of item queries with condition.

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
