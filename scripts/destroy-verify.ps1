param(
    [string]$Region = "us-east-1",
    [string]$ClusterName = "startracker-eks-dev",
    [string]$NameFilter = "startracker"
)

$ErrorActionPreference = "Stop"
$script:PermissionIssues = @()

function Write-Check {
    param(
        [string]$Title,
        [bool]$Ok,
        [string]$Details
    )

    if ($Ok) {
        Write-Host "[OK]  $Title" -ForegroundColor Green
    } else {
        Write-Host "[WARN] $Title" -ForegroundColor Yellow
    }
    if ($Details) {
        Write-Host "      $Details"
    }
}

function Query-Json {
    param([string]$Command)

    $output = Invoke-Expression "$Command 2>&1"
    $text = ($output | Out-String).Trim()

    if ($text -match "AccessDenied|AccessDeniedException|UnauthorizedOperation|not authorized to perform") {
        $script:PermissionIssues += $text
        return @()
    }

    if ([string]::IsNullOrWhiteSpace($text)) { return @() }
    try {
        return $text | ConvertFrom-Json
    } catch {
        return @()
    }
}

Write-Host "Destroy verification started"
Write-Host "Region: $Region"
Write-Host "Cluster: $ClusterName"
Write-Host "Filter: $NameFilter"
Write-Host ""

# 1) EKS clusters
$clusters = Query-Json "aws eks list-clusters --region $Region --output json"
$clusterExists = $false
if ($clusters.clusters) {
    $clusterExists = $clusters.clusters -contains $ClusterName
}
Write-Check -Title "EKS cluster deleted" -Ok (-not $clusterExists) -Details ($(if ($clusterExists) { "Cluster still exists: $ClusterName" } else { "Cluster not found." }))

# 2) ELBv2 load balancers
$lbs = Query-Json "aws elbv2 describe-load-balancers --region $Region --output json"
$matchedLbs = @()
if ($lbs.LoadBalancers) {
    $matchedLbs = $lbs.LoadBalancers | Where-Object {
        $_.LoadBalancerName -like "*$NameFilter*" -or $_.DNSName -like "*$NameFilter*"
    }
}
Write-Check -Title "ALBs/NLBs removed" -Ok ($matchedLbs.Count -eq 0) -Details ($(if ($matchedLbs.Count -gt 0) { ($matchedLbs | ForEach-Object { $_.LoadBalancerArn }) -join ", " } else { "No matching load balancers." }))

# 3) Target groups
$tgs = Query-Json "aws elbv2 describe-target-groups --region $Region --output json"
$matchedTgs = @()
if ($tgs.TargetGroups) {
    $matchedTgs = $tgs.TargetGroups | Where-Object { $_.TargetGroupName -like "*$NameFilter*" -or $_.TargetGroupName -like "k8s*" }
}
Write-Check -Title "Target groups removed" -Ok ($matchedTgs.Count -eq 0) -Details ($(if ($matchedTgs.Count -gt 0) { ($matchedTgs | ForEach-Object { $_.TargetGroupArn }) -join ", " } else { "No matching target groups." }))

# 4) EC2 instances
$instances = Query-Json "aws ec2 describe-instances --region $Region --filters Name=instance-state-name,Values=pending,running,stopping,stopped --output json"
$matchedInstances = @()
if ($instances.Reservations) {
    foreach ($reservation in $instances.Reservations) {
        foreach ($instance in $reservation.Instances) {
            $tagMatch = $false
            if ($instance.Tags) {
                $tagMatch = $instance.Tags | Where-Object { $_.Value -like "*$NameFilter*" -or $_.Key -like "*$NameFilter*" }
            }
            if ($tagMatch) {
                $matchedInstances += $instance
            }
        }
    }
}
Write-Check -Title "EC2 instances removed" -Ok ($matchedInstances.Count -eq 0) -Details ($(if ($matchedInstances.Count -gt 0) { ($matchedInstances | ForEach-Object { $_.InstanceId }) -join ", " } else { "No matching EC2 instances." }))

# 5) EBS volumes
$volumes = Query-Json "aws ec2 describe-volumes --region $Region --filters Name=status,Values=available --output json"
$matchedVolumes = @()
if ($volumes.Volumes) {
    foreach ($volume in $volumes.Volumes) {
        $tagMatch = $false
        if ($volume.Tags) {
            $tagMatch = $volume.Tags | Where-Object { $_.Value -like "*$NameFilter*" -or $_.Key -like "*$NameFilter*" }
        }
        if ($tagMatch) {
            $matchedVolumes += $volume
        }
    }
}
Write-Check -Title "Orphan EBS volumes removed" -Ok ($matchedVolumes.Count -eq 0) -Details ($(if ($matchedVolumes.Count -gt 0) { ($matchedVolumes | ForEach-Object { $_.VolumeId }) -join ", " } else { "No matching available EBS volumes." }))

# 6) NAT gateways
$nat = Query-Json "aws ec2 describe-nat-gateways --region $Region --output json"
$matchedNat = @()
if ($nat.NatGateways) {
    $matchedNat = $nat.NatGateways | Where-Object {
        $_.State -ne "deleted" -and (
            ($_.Tags | Where-Object { $_.Value -like "*$NameFilter*" -or $_.Key -like "*$NameFilter*" }) -or
            $_.NatGatewayId -like "*$NameFilter*"
        )
    }
}
Write-Check -Title "NAT gateways removed" -Ok ($matchedNat.Count -eq 0) -Details ($(if ($matchedNat.Count -gt 0) { ($matchedNat | ForEach-Object { $_.NatGatewayId }) -join ", " } else { "No matching active NAT gateways." }))

# 7) Unattached Elastic IPs
$eips = Query-Json "aws ec2 describe-addresses --region $Region --output json"
$matchedEips = @()
if ($eips.Addresses) {
    foreach ($addr in $eips.Addresses) {
        if ($addr.AssociationId) { continue }
        $tagMatch = $false
        if ($addr.Tags) {
            $tagMatch = $addr.Tags | Where-Object { $_.Value -like "*$NameFilter*" -or $_.Key -like "*$NameFilter*" }
        }
        if ($tagMatch) {
            $matchedEips += $addr
        }
    }
}
Write-Check -Title "Unattached Elastic IPs removed" -Ok ($matchedEips.Count -eq 0) -Details ($(if ($matchedEips.Count -gt 0) { ($matchedEips | ForEach-Object { $_.AllocationId }) -join ", " } else { "No matching unattached Elastic IPs." }))

# 8) CloudFormation stacks
$stacks = Query-Json "aws cloudformation list-stacks --region $Region --output json"
$matchedStacks = @()
if ($stacks.StackSummaries) {
    $matchedStacks = $stacks.StackSummaries | Where-Object {
        $_.StackStatus -ne "DELETE_COMPLETE" -and (
            $_.StackName -like "*$ClusterName*" -or $_.StackName -like "*$NameFilter*"
        )
    }
}
Write-Check -Title "CloudFormation stacks deleted" -Ok ($matchedStacks.Count -eq 0) -Details ($(if ($matchedStacks.Count -gt 0) { ($matchedStacks | ForEach-Object { "$($_.StackName):$($_.StackStatus)" }) -join ", " } else { "No matching active stacks." }))

# 9) ECR repositories
$repos = Query-Json "aws ecr describe-repositories --region $Region --output json"
$matchedRepos = @()
if ($repos.repositories) {
    $matchedRepos = $repos.repositories | Where-Object { $_.repositoryName -like "*$NameFilter*" }
}
Write-Check -Title "ECR repos reviewed" -Ok ($matchedRepos.Count -eq 0) -Details ($(if ($matchedRepos.Count -gt 0) { "Repos still present: " + (($matchedRepos | ForEach-Object { $_.repositoryName }) -join ", ") } else { "No matching repos found." }))

# 10) S3 buckets and objects
$buckets = Query-Json "aws s3api list-buckets --output json"
$matchedBuckets = @()
if ($buckets.Buckets) {
    $matchedBuckets = $buckets.Buckets | Where-Object {
        $_.Name -like "*$NameFilter*" -or $_.Name -like "startracker-ui-*"
    }
}

$nonEmptyBuckets = @()
foreach ($bucket in $matchedBuckets) {
    $bucketName = $bucket.Name
    try {
        $objectsJson = aws s3api list-objects-v2 --bucket $bucketName --max-items 1 --output json 2>$null
        if (-not [string]::IsNullOrWhiteSpace($objectsJson)) {
            $objects = $objectsJson | ConvertFrom-Json
            if ($objects.KeyCount -gt 0) {
                $nonEmptyBuckets += $bucketName
            }
        }
    } catch {
        # Keep bucket in matched list even if object listing fails.
    }
}

Write-Check -Title "S3 buckets removed" -Ok ($matchedBuckets.Count -eq 0) -Details ($(if ($matchedBuckets.Count -gt 0) { "Buckets still present: " + (($matchedBuckets | ForEach-Object { $_.Name }) -join ", ") } else { "No matching S3 buckets found." }))
Write-Check -Title "S3 buckets empty" -Ok ($nonEmptyBuckets.Count -eq 0) -Details ($(if ($nonEmptyBuckets.Count -gt 0) { "Non-empty buckets: " + ($nonEmptyBuckets -join ", ") } else { "No non-empty matching buckets." }))

Write-Host ""
if ($script:PermissionIssues.Count -gt 0) {
    Write-Host "[WARN] Verification was limited by missing IAM read permissions." -ForegroundColor Yellow
    Write-Host "      Grant read-only describe/list permissions for full teardown verification coverage."
}
Write-Host "Destroy verification complete."
Write-Host "Tip: adjust -NameFilter to avoid false positives."
exit 0
