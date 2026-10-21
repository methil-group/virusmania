param([string]$Action,[string]$Name)
$repository = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$project = Join-Path $repository 'unity'
$script = Join-Path $PSScriptRoot 'VisualReview.cs'
if($Action) {
  $arguments = ConvertTo-Json -InputObject @($Action) -Compress
  $result = unity command run_script --file $script --entry VisualReview.Run --args $arguments --project-path $project --format json | ConvertFrom-Json
  if(!$result.success -or !$result.data.result.success) { $result | ConvertTo-Json -Depth 12; exit 1 }
  $result.data.result.result | ConvertTo-Json -Depth 8 -Compress
}
if($Name) {
  $arguments = ConvertTo-Json -InputObject @($Name) -Compress
  $capture = unity command run_script --file $script --entry VisualReview.Capture --args $arguments --project-path $project --format json | ConvertFrom-Json
  if(!$capture.success -or !$capture.data.result.success) { $capture | ConvertTo-Json -Depth 10; exit 1 }
  Write-Output "Captured $Name"
}
