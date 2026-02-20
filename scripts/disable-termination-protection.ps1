param(
    [string]$Region = "us-east-1",
    [string]$ClusterName = "startracker-eks-dev"
)

$ErrorActionPreference = "Stop"

Write-Host "Disabling CloudFormation termination protection for eksctl stacks..."
Write-Host "Region: $Region"
Write-Host "Cluster: $ClusterName"

$stacksJson = aws cloudformation list-stacks `
  --region $Region `
  --stack-status-filter CREATE_IN_PROGRESS CREATE_COMPLETE UPDATE_IN_PROGRESS UPDATE_COMPLETE ROLLBACK_IN_PROGRESS ROLLBACK_COMPLETE UPDATE_ROLLBACK_IN_PROGRESS UPDATE_ROLLBACK_COMPLETE `
  --output json

$stacks = ($stacksJson | ConvertFrom-Json).StackSummaries |
  Where-Object { $_.StackName -like "*eksctl-$ClusterName*" }

if (-not $stacks -or $stacks.Count -eq 0) {
    Write-Host "No matching eksctl stacks found."
    exit 0
}

foreach ($stack in $stacks) {
    Write-Host "Updating stack: $($stack.StackName)"
    aws cloudformation update-termination-protection `
      --region $Region `
      --stack-name $stack.StackName `
      --no-enable-termination-protection | Out-Null
}

Write-Host "Done. Termination protection disabled for matching stacks."