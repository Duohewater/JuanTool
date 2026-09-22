# Regenerate the Windows icon after changing JuanToolIcon in Assets/Icons.xaml.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore,PresentationFramework,WindowsBase
$projectRoot = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $projectRoot 'src\JuanTool\Assets'
$resources = [Windows.Markup.XamlReader]::Parse([IO.File]::ReadAllText((Join-Path $assets 'Icons.xaml')))
$drawing = $resources['JuanToolIcon'].Drawing
$sizes = @(16,20,24,32,40,48,64,128,256)
$frames = foreach ($size in $sizes) {
    $visual = New-Object Windows.Media.DrawingVisual
    $context = $visual.RenderOpen()
    $context.PushTransform((New-Object Windows.Media.ScaleTransform(($size / 256.0), ($size / 256.0))))
    $context.DrawDrawing($drawing)
    $context.Pop()
    $context.Close()
    $bitmap = New-Object Windows.Media.Imaging.RenderTargetBitmap($size,$size,96,96,[Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = New-Object Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $buffer = New-Object IO.MemoryStream
    try { $encoder.Save($buffer); ,$buffer.ToArray() } finally { $buffer.Dispose() }
}
$stream = [IO.File]::Create((Join-Path $assets 'juantool.ico'))
$writer = New-Object IO.BinaryWriter($stream)
try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($index = 0; $index -lt $sizes.Count; $index++) {
        $dimension = if ($sizes[$index] -eq 256) { 0 } else { $sizes[$index] }
        $writer.Write([byte]$dimension)
        $writer.Write([byte]$dimension)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$frames[$index].Length)
        $writer.Write([uint32]$offset)
        $offset += $frames[$index].Length
    }
    foreach ($frame in $frames) { $writer.Write([byte[]]$frame) }
} finally { $writer.Dispose() }
Write-Host 'Generated Assets/juantool.ico (16-256 px).'
