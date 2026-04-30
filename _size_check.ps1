$objects = git -C C:\Dev\ppbm_project rev-list --objects --all
$batch = $objects | git -C C:\Dev\ppbm_project cat-file --batch-check='%(objecttype) %(objectsize) %(rest)'
$blobs = $batch | Where-Object { $_.StartsWith('blob') }
$results = foreach ($line in $blobs) {
    $parts = $line.Trim() -split ' ', 3
    [PSCustomObject]@{MB=[math]::Round([int]$parts[1]/1MB,2); Path=$parts[2]}
}
$results | Sort-Object MB -Descending | Select-Object -First 20 | Format-Table -AutoSize
