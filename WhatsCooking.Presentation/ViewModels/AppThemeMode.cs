namespace WhatsCooking.ViewModels;

/// <summary>
/// Defines the available visual theme modes for the application.
/// </summary>
internal enum AppThemeMode
{
    /// <summary>
    /// Follows the current Windows app theme preference.
    /// </summary>
    Os = 0,

    /// <summary>
    /// Uses the light palette.
    /// </summary>
    Light = 1,

    /// <summary>
    /// Uses the default dark palette.
    /// </summary>
    Dark = 2,

    /// <summary>
    /// Uses a muted forest-inspired dark palette.
    /// </summary>
    Forest = 3,

    /// <summary>
    /// Uses a warm autumn-inspired dark palette.
    /// </summary>
    Autumn = 4,

    /// <summary>
    /// Uses a dark pink palette.
    /// </summary>
    DarkPink = 5,

    /// <summary>
    /// Uses a Matrix-inspired neon green dark palette.
    /// </summary>
    Matrix = 6,

    /// <summary>
    /// Uses a Visual Studio-inspired coding palette.
    /// </summary>
    Code = 7,

    /// <summary>
    /// Uses a neon cyberpunk palette.
    /// </summary>
    Cyberpunk = 8,

    /// <summary>
    /// Uses a deep-sea palette.
    /// </summary>
    DeepSea = 9,

    /// <summary>
    /// Uses a translucent glass-like dark palette.
    /// </summary>
    Glass = 10,

    /// <summary>
    /// Uses a misty alpine dawn palette.
    /// </summary>
    AlpineDawn = 11
}
