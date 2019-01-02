# Toggl endpoints
These are endpoints that are prefixed with `https://mobile.toggl.com/api/v9/` (`mobile.toggl.space` domain is used when building the app in debug mode [or for testing]).

## Authentication

* `GET me`
  * `app_name` might be set to `toggl_mobile`, when using google login
* `POST signup`
  * `app_name` might be set to `toggl_mobile`, when using google login
* `PUT me`
* `POST me/lost_passwords`
* `GET me/logged`

## Workspaces

* `GET me/workspaces`
* `GET me/workspaces/{workspace_id}` - get workspace by id

## Clients

* `GET me/clients`
  * `since` might be set in some cases
* `POST workspaces/{workspace_id}/clients` - create a new client

## Projects

* `GET me/projects`
  * `since` might be set in some cases
* `POST workspaces/{workspace_id}/projects`- create a new project

## Tasks

* `GET me/tasks`
  * `since` might be set in some cases
* `POST workspaces/{workspace_id}/projects/{project_id}/tasks` - create a new task

## Time entries

* `GET me/time_entries`
  * `since` might be set in some cases
* `POST workspaces/{workspace_id}/time_entries` - create a new time entry
* `PUT workspaces/{workspace_id}/time_entries/{time_entry_id}` - edit time entry
* `DELETE workspaces/{workspace_id}/time_entries/{time_entry_id}`

## Tags

* `GET me/tags`
  * `since` might be set in some cases
* `POST workspaces/{workspace_id}/tags` - create a new tag

## Preferences

* `GET me/preferences`
* `POST me/preferences`

## Countries

* `GET countries`

## Location

* `GET me/location`

## Other

* `GET me/features`

## Subscriptions

* `GET /workspaces/{user.DefaultWorkspaceId}/plans` - get a list of available plans for integration tests
* `POST /workspaces/{workspaceId}/subscription` - set a specific plan (trial) for integration tests

## Feedback

* `POST /mobile/feedback`

# Reports endpoints
These endpoints are prefixed with `https://mobile.toggl.com/reports/api/v3/` (`mobile.toggl.space` domain is used when building the app in debug mode [or for testing]).

* `POST workspace/{workspace_id}/projects/summary`
* `POST workspace/{workspace_id}/search/projects`
