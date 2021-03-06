﻿

#pragma warning disable 1591 // this file isn't technically autogenerated, but we don't expect to document every single EMsg

namespace SteamKit2.GC
{
    public abstract class EGCMsgBase
    {
        public const uint GenericReply = 10;

        public const uint SOCreate = 21;
        public const uint SOUpdate = 22;
        public const uint SODestroy = 23;
        public const uint SOCacheSubscribed = 24;
        public const uint SOCacheUnsubscribed = 25;
        public const uint SOUpdateMultiple = 26;
        public const uint SOCacheSubscriptionCheck = 27;
        public const uint SOCacheSubscriptionRefresh = 28;

        public const uint AchievementAwarded = 51;
        public const uint ConCommand = 52;
        public const uint StartPlaying = 53;
        public const uint StopPlaying = 54;
        public const uint StartGameserver = 55;
        public const uint StopGameserver = 56;
        public const uint WGRequest = 57;
        public const uint WGResponse = 58;
        public const uint GetUserGameStatsSchema = 59;
        public const uint GetUserGameStatsSchemaResponse = 60;
        public const uint GetUserStatsDEPRECATED = 61;
        public const uint GetUserStatsResponse = 62;
        public const uint AppInfoUpdated = 63;
        public const uint ValidateSession = 64;
        public const uint ValidateSessionResponse = 65;
        public const uint LookupAccountFromInput = 66;
        public const uint SendHTTPRequest = 67;
        public const uint SendHTTPRequestResponse = 68;
        public const uint PreTestSetup = 69;
        public const uint RecordSupportAction = 70;
        public const uint GetAccountDetails = 71;
        public const uint SendInterAppMessage = 72;
        public const uint ReceiveInterAppMessage = 73;
        public const uint FindAccounts = 74;
        public const uint PostAlert = 75;
        public const uint GetLicenses = 76;
        public const uint GetUserStats = 77;
        public const uint GetCommands = 78;
        public const uint GetCommandsResponse = 79;
        public const uint AddFreeLicense = 80;
        public const uint AddFreeLicenseResponse = 81;
        public const uint GetIPLocation = 82;
        public const uint GetIPLocationResponse = 83;
        public const uint SystemStatsSchema = 84;
        public const uint GetSystemStats = 85;
        public const uint GetSystemStatsResponse = 86;

        public const uint WebAPIRegisterInterfaces = 101;
        public const uint WebAPIJobRequest = 102;
        public const uint WebAPIRegistrationRequested = 103;

        public const uint MemCachedGet = 200;
        public const uint MemCachedGetResponse = 201;
        public const uint MemCachedSet = 203;
        public const uint MemCachedDelete = 204;

        public const uint SetItemPosition = 1001;
        public const uint Craft = 1002;
        public const uint CraftResponse = 1003;
        public const uint Delete = 1004;
        public const uint VerifyCacheSubscription = 1005;
        public const uint NameItem = 1006;
        public const uint UnlockCrate = 1007;
        public const uint UnlockCrateResponse = 1008;
        public const uint PaintItem = 1009;
        public const uint PaintItemResponse = 1010;
        public const uint GoldenWrenchBroadcast = 1011;
        public const uint MOTDRequest = 1012;
        public const uint MOTDRequestResponse = 1013;
        public const uint AddItemToSocket = 1014;
        public const uint AddItemToSocketResponse = 1015;
        public const uint AddSocketToBaseItem = 1016;
        public const uint AddSocketToItem = 1017;
        public const uint AddSocketToItemResponse = 1018;
        public const uint NameBaseItem = 1019;
        public const uint NameBaseItemResponse = 1020;
        public const uint RemoveSocketItem = 1021;
        public const uint RemoveSocketItemResponse = 1022;
        public const uint CustomizeItemTexture = 1023;
        public const uint CustomizeItemTextureResponse = 1024;
        public const uint UseItemRequest = 1025;
        public const uint UseItemResponse = 1026;
        public const uint GiftedItems = 1027;
        public const uint SpawnItem = 1028;
        public const uint RespawnPostLoadoutChange = 1029;
        public const uint RemoveItemName = 1030;
        public const uint RemoveItemPaint = 1031;
        public const uint GiftWrapItem = 1032;
        public const uint GiftWrapItemResponse = 1033;
        public const uint DeliverGift = 1034;
        public const uint DeliverGiftResponseGiver = 1035;
        public const uint DeliverGiftResponseReceiver = 1036;
        public const uint UnwrapGiftRequest = 1037;
        public const uint UnwrapGiftResponse = 1038;
        public const uint SetItemStyle = 1039;
        public const uint UsedClaimCodeItem = 1040;
        public const uint SortItems = 1041;
        public const uint RevolvingLootList = 1042;
        public const uint LookupAccount = 1043;
        public const uint LookupAccountResponse = 1044;
        public const uint LookupAccountName = 1045;
        public const uint LookupAccountNameResponse = 1046;
        public const uint UpdateItemSchema = 1049;
        public const uint RequestInventoryRefresh = 1050;
        public const uint RemoveCustomTexture = 1051;
        public const uint RemoveCustomTextureResponse = 1052;
        public const uint RemoveItemMakersMark = 1053;
        public const uint RemoveItemMakersMarkResponse = 1054;
        public const uint RemoveUniqueCraftIndex = 1055;
        public const uint RemoveUniqueCraftIndexResponse = 1056;
        public const uint SaxxyBroadcast = 1057;
        public const uint BackpackSortFinished = 1058;
        public const uint AdjustItemEquippedState = 1059;
        public const uint RequestItemSchemaData = 1060;
        public const uint ApplyConsumableEffects = 1069;
        public const uint ConsumableExhausted = 1070;
        public const uint ShowItemsPickedUp = 1071;
        
        public const uint ApplyStrangePart = 1073;

        public const uint Trading_InitiateTradeRequest = 1501;
        public const uint Trading_InitiateTradeResponse = 1502;
        public const uint Trading_StartSession = 1503;
        public const uint Trading_SetItem = 1504;
        public const uint Trading_RemoveItem = 1505;
        public const uint Trading_UpdateTradeInfo = 1506;
        public const uint Trading_SetReadiness = 1507;
        public const uint Trading_ReadinessResponse = 1508;
        public const uint Trading_SessionClosed = 1509;
        public const uint Trading_CancelSession = 1510;
        public const uint Trading_TradeChatMsg = 1511;
        public const uint Trading_ConfirmOffer = 1512;
        public const uint Trading_TradeTypingChatMsg = 1513;

        public const uint ServerBrowser_FavoriteServer = 1601;
        public const uint ServerBrowser_BlacklistServer = 1602;

        public const uint CheckItemPreviewStatus = 1701;

        public const uint Dev_NewItemRequest = 2001;
        public const uint Dev_NewItemReqeustResponse = 2002;

        public const uint StoreGetUserData = 2500;
        public const uint StoreGetUserDataResponse = 2501;
        public const uint StorePurchaseInitDEPRECATED = 2502;
        public const uint StorePurchaseInitResponseDEPRECATED = 2503;
        public const uint StorePurchaseFinalize = 2504;
        public const uint StorePurchaseFinalizeResponse = 2505;
        public const uint StorePurchaseCancel = 2506;
        public const uint StorePurchaseCancelResponse = 2507;
        public const uint StorePurchaseQueryTxn = 2508;
        public const uint StorePurchaseQueryTxnResponse = 2509;
        public const uint StorePurchaseInit = 2510;
        public const uint StorePurchaseInitResponse = 2511;

        public const uint SystemMessage = 4001;
        public const uint ReplicateConVars = 4002;
        public const uint ConVarUpdated = 4003;
        public const uint ClientWelcome = 4004;
        public const uint ClientHello = 4006;
        public const uint InQueue = 4008;

        public const uint InviteToParty = 4501;
        public const uint InvitationCreated = 4502;
        public const uint PartyInviteResponse = 4503;
        public const uint KickFromParty = 4504;
        public const uint LeaveParty = 4505;

        public const uint GCError = 4509;
    }
}

#pragma warning restore 1591