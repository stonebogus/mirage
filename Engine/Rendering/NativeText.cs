using System.Runtime.InteropServices;

namespace Mirage.Rendering;

internal static partial class NativeText
{
    private const string Sdl = "SDL3";
    private const string SdlTtf = "libSDL3_ttf.so.0";

    [LibraryImport(Sdl, EntryPoint = "SDL_IOFromConstMem")]
    internal static partial nint IOFromConstMem(nint data, nuint size);

    [LibraryImport(Sdl, EntryPoint = "SDL_CloseIO")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool CloseIO(nint stream);

    [LibraryImport(Sdl, EntryPoint = "SDL_CreateTextureFromSurface")]
    internal static partial nint CreateTextureFromSurface(nint renderer, nint surface);

    [LibraryImport(SdlTtf, EntryPoint = "TTF_Init")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool Init();

    [LibraryImport(SdlTtf, EntryPoint = "TTF_Quit")]
    internal static partial void Quit();

    [LibraryImport(SdlTtf, EntryPoint = "TTF_OpenFontIO")]
    internal static partial nint OpenFontIO(
        nint stream,
        [MarshalAs(UnmanagedType.I1)] bool closeIO,
        float size
    );

    [LibraryImport(SdlTtf, EntryPoint = "TTF_CloseFont")]
    internal static partial void CloseFont(nint font);

    [LibraryImport(SdlTtf, EntryPoint = "TTF_SetFontStyle")]
    internal static partial void SetFontStyle(nint font, uint style);

    [LibraryImport(
        SdlTtf,
        EntryPoint = "TTF_GetStringSize",
        StringMarshalling = StringMarshalling.Utf8
    )]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool GetStringSize(
        nint font,
        string text,
        nuint length,
        out int width,
        out int height
    );

    [LibraryImport(
        SdlTtf,
        EntryPoint = "TTF_RenderText_Blended",
        StringMarshalling = StringMarshalling.Utf8
    )]
    internal static partial nint RenderTextBlended(
        nint font,
        string text,
        nuint length,
        NativeColor color
    );

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeColor(byte r, byte g, byte b, byte a)
    {
        public readonly byte R = r;
        public readonly byte G = g;
        public readonly byte B = b;
        public readonly byte A = a;
    }
}
