import { DaprServer } from "@dapr/dapr";

import ScrapperActor from "./scrapper-actor";

const DAPR_HOST = process.env.DAPR_HOST || "http://localhost";
const SERVER_PORT = process.env.PORT || process.env.SERVER_PORT || "5002";

async function main() {
  const server = new DaprServer(undefined, SERVER_PORT, DAPR_HOST);

  await server.actor.init(); // Let the server know we need actors
  await server.actor.registerActor(ScrapperActor); // Register the actor
  await server.start();
  console.log(`scrapper-actor::started on port ${SERVER_PORT}`)
}

main().catch((e) => console.error(e));
