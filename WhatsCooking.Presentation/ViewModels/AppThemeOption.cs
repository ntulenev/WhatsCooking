namespace WhatsCooking.ViewModels;

/// <summary>
/// Describes a selectable application theme option and its UI label.
/// </summary>
/// <param name="Mode">The theme mode represented by the option.</param>
/// <param name="Label">The label shown to the user.</param>
internal readonly record struct AppThemeOption(AppThemeMode Mode, string Label);

/// <summary>
/// Exposes the full set of theme options supported by the application UI.
/// </summary>
internal static class AppThemeOptions
{
    /// <summary>
    /// Gets every theme option in the order it should appear in menus.
    /// </summary>
    public static IReadOnlyList<AppThemeOption> All { get; } =
    [
        new(AppThemeMode.Os, "OS"),
        new(AppThemeMode.Light, "Light"),
        new(AppThemeMode.Glass, "Glass"),
        new(AppThemeMode.Dark, "Dark"),
        new(AppThemeMode.Forest, "Forest"),
        new(AppThemeMode.Autumn, "Autumn"),
        new(AppThemeMode.DarkPink, "Dark Pink"),
        new(AppThemeMode.Matrix, "Matrix"),
        new(AppThemeMode.Code, "Code"),
        new(AppThemeMode.Cyberpunk, "Cyberpunk"),
        new(AppThemeMode.DeepSea, "Deep sea"),
        new(AppThemeMode.AlpineDawn, "Alpine dawn")
    ];
}
