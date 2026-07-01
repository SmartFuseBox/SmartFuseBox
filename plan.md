Move SensorMetaCache refresh to happen immediately when the DashboardPoller starts executing (after connection), rather than deferred until the first successful poll response.

Changes to `PowerControlHubApp\Services\DashboardPoller.cs`:
1. Remove the `_wasPreviouslyConnected` flag and the fire-and-forget `_metaCache.RefreshAsync` call inside the poll loop.
2. In `ExecuteAsync`, before entering the `while` loop, call `_metaCache.RefreshAsync(_service, stoppingToken)` synchronously (await) once. This ensures metadata is available before any sensor detail pages can be navigated to.
3. This makes the meta cache refresh happen once per connection, shortly after the poller starts, aligned with the device being reachable.
