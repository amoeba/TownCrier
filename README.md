# TownCrier

![](docs/discord-webhook-example.png)

A webhook plugin for Decal. Send ingame events (such as dying) to a webhook (such as a Discord channel). TownCrier can also send webhooks on timed intervals (e.g., sending your unassigned XP every hour).

## What is a webhook?

A webhook is a way for services to talk to one another.
This is done by one service sending an HTTP request of some sort to a receiver and the receiver then does _something_ with it.
A familiar example is hooking up a Discord channel to a service like GitHub/GitLab.

## Installation

Until I release the first version, you'll have to install the plugin manually.

- Open Decal
- Click Add (updating if it asks you)
- Click Browse
- Find TownCrier.dll and click Save

## Getting started

The UI is pretty basic at this point and I hope to make it easier to use eventually.
To get your first webhook set up, you need to do two things:

1. Create a webhook (in the Webhooks tab) (do this first)
2. Create an action (in the Actions tab)

### Creating a webhook

This guide assumes you're at least somewhat familiar with webhooks or at least the technology involved.

**Note: The URL and Payload fields are templates and use a wildcard (an `@`) to fill in the appropriate message so make sure you have an `@` where your message should go.**

1. Provide a name for your webhook. It must be unique.
2. Provide a URL for your webhook

	Some webhook providers use query parameters (http://example.com/?foo=bar) instead of JSON payloads to send webhook parameters.
	For these cases, put an `@` symbol in the URL where your message should go.
	For example, a webhook for Zapier can look like `https://hooks.zapier.com/hooks/catch/123456/abcdefg/?message=@`, note the `@`.
	If the ingame message is "You died.", TownCrier would send a GET request to `https://hooks.zapier.com/hooks/catch/123456/abcdefg/?message=You%20died`.

3. Provide an HTTP method (GET or POST) for your webhook
4. Optional. Provide a JSON payload (only for POST webhooks). There's no need to fill this in for GET webhooks.

	Similar to how the `@` gets used in the URL for GET webhooks, enter a JSON template with an `@` where your message should go.
	For example, a Discord webhook payload would could like `{"content": "@"}` and the `@` would get replaced by the ingame message.
5. Click Add Webhook

### Creating an action

Creating an action binds an ingame event to a particular webhook.
A single webhook can be bound to multiple ingame events.

1. Pick an Event (e.g., You log in)
2. Pick a Webhook
3. Click Add Action

### Creating a timer

Creating a timer causes the desired webhook to trigger at the time interval (in minutes) you specify.

1. Provide a timer interval (in minutes). e.g., a value of '1' means your webook will trigger every minute.
2. Pick a Webhook
3. Enter a message:

	In your Message, you can make use of special variables to send yourself information about your character. Each variable starts with a $. Available variables are $NAME, $LEVEL, $UXP (unassigned xp), $TXP (total XP), $HEALTH, $STAMINA, $MANA, and $VITAE. 
	
	Example: "Hello, $NAME, your vitae is $VITAE".