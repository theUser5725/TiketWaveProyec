#!/usr/bin/env pwsh
# Recreate Postgres container and volume, then verify DB, user and tables
# Usage: Run this from the repo root in PowerShell (pwsh or Windows PowerShell)

Write-Host 'Stopping containers and removing postgres volume (this will delete DB data)...'
docker-compose down -v

Write-Host 'Starting containers...'
docker-compose up -d

Write-Host 'Waiting for Postgres to become ready...'
# Wait for postgres to accept connections
$max = 60
$ok = $false
for ($i = 0; $i -lt $max; $i++) {
    try {
        docker exec tiketwave-db pg_isready -U tkwaver -d tkwaver_db | Out-Null
        $ok = $true
        break
    } catch {
        Start-Sleep -Seconds 1
    }
}

if (-not $ok) {
    Write-Error 'Postgres did not become ready after {0} seconds' -f $max
    exit 1
}

Write-Host 'Postgres ready - listing tables in tkwaver_db as user tkwaver'
# Password for the tkwaver user (matches docker-compose)
$pw = 'tkdpsw987'

# List tables
docker exec -e PGPASSWORD=$pw tiketwave-db psql -U tkwaver -d tkwaver_db -c '\\dt'

Write-Host 'If you need to connect from your host machine use:'
Write-Host '  host=localhost port=5432 dbname=tkwaver_db user=tkwaver password=tkdpsw987'
