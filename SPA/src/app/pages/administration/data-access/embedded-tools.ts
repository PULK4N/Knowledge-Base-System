export interface EmbeddedTool {
  readonly name: string;
  readonly description: string;
  readonly url: string;
  readonly frameTitle: string;
}

/**
 * Both tools run in their own containers and are reverse-proxied under the SPA
 * origin, so the frames stay same-origin and the Artemis console can be framed.
 */
export const DATABASE_TOOL: EmbeddedTool = {
  name: 'PostgreSQL',
  description:
    'Browse tables, run read queries, and inspect the event store and read models.',
  url: '/pgweb/',
  frameTitle: 'PostgreSQL database browser',
};

export const QUEUE_TOOL: EmbeddedTool = {
  name: 'Message queues',
  description:
    'Inspect Artemis addresses, queues, consumers, and pending messages.',
  url: '/console/',
  frameTitle: 'ActiveMQ Artemis console',
};
