// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Win32.Foundation;
using System.Globalization;

namespace Snap.Hutao.Remastered.Service.Game.Island;

public struct IslandEnvironment
{
#pragma warning disable CS0649
    public IslandEnvironmentView View;
#pragma warning restore CS0649
    public BOOL IsOversea;
    public BOOL ProvideOffsets;

    public BOOL EnableSetFieldOfView;
    public float FieldOfView;
    public BOOL DisablePlayerPerspective;
    public BOOL DisableFog;
    public BOOL EnableSetTargetFrameRate;
    public int TargetFrameRate;
    public BOOL RemoveOpenTeamProgress;
    public BOOL HideQuestBanner;
    public BOOL DisableEventCameraMove;
    public BOOL DisableShowDamageText;
    public BOOL UsingTouchScreen;
    public BOOL RedirectCombineEntry;
    public BOOL ResinListItemId000106Allowed;
    public BOOL ResinListItemId000201Allowed;
    public BOOL ResinListItemId107009Allowed;
    public BOOL ResinListItemId107012Allowed;
    public BOOL ResinListItemId220007Allowed;
    
    public BOOL DisplayPaimon;
    public BOOL DebugMode;
    public BOOL HidePlayerInfo;
    public BOOL HideGrass;
    public BOOL GamepadHotSwitchEnabled;
    public BOOL EnableInLevelClockPageSpeedUp;
    public int CombineHotkey;
    public BOOL WeakMapCheck;
    public BOOL DisablePlayerDiveMosaic;
    public BOOL FastSkipTalk;
}

public class HexStringToNintConverter : JsonConverter<nint>
{
    public override nint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? hexString = reader.GetString();
        if (hexString == null)
        {
            return 0;
        }

        if (hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            hexString = hexString.Substring(2);
        }

        long longValue = long.Parse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return (nint)longValue;
    }

    public override void Write(Utf8JsonWriter writer, nint value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"0x{value:X}");
    }
}
