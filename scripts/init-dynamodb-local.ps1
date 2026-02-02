# Initialize DynamoDB Local table for StarTracker
# Run this script before starting the API with DynamoDB Local configuration

$tableName = "observations"
$endpoint = "http://localhost:8000"
$region = "us-east-1"

Write-Host "Creating DynamoDB table: $tableName" -ForegroundColor Cyan

aws dynamodb create-table `
    --table-name $tableName `
    --attribute-definitions `
        AttributeName=Id,AttributeType=S `
        AttributeName=Target,AttributeType=S `
        AttributeName=ObservedAt,AttributeType=N `
    --key-schema `
        AttributeName=Id,KeyType=HASH `
    --global-secondary-indexes `
        "IndexName=TargetIndex,KeySchema=[{AttributeName=Target,KeyType=HASH},{AttributeName=ObservedAt,KeyType=RANGE}],Projection={ProjectionType=ALL},ProvisionedThroughput={ReadCapacityUnits=5,WriteCapacityUnits=5}" `
    --provisioned-throughput `
        ReadCapacityUnits=5,WriteCapacityUnits=5 `
    --endpoint-url $endpoint `
    --region $region

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Table created successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Verify with:" -ForegroundColor Yellow
    Write-Host "  aws dynamodb describe-table --table-name $tableName --endpoint-url $endpoint --region $region" -ForegroundColor Gray
} else {
    Write-Host "✗ Failed to create table" -ForegroundColor Red
}
