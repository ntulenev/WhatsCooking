namespace WhatsCooking.Services;

/// <summary>
/// Writes text to the operating system clipboard.
/// </summary>
internal interface IClipboardService
{
    /// <summary>
    /// Replaces the clipboard contents with text.
    /// </summary>
    /// <param name="text">Text to copy.</param>
    void SetText(string text);
}
