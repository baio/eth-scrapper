import { AbstractActor } from '@dapr/dapr';
import axios from 'axios';
import { Data as HandlerData, Entry, Error, handle, Result, Success } from './handler';
const _ = require('lodash');

//

interface RequestBlockRange {
  from?: number;
  to?: number;
}

interface Data {
  ethProviderUrl: string;
  contractAddress: string;
  abi: string;
  blockRange: RequestBlockRange;
}

const mapRequestBlockRange = (range: RequestBlockRange) => ({
  from: range.from ? { Some: [range.from] } : null,
  to: range.to ? { Some: [range.to] } : null,
});

const mapEntry = (entry: Entry) => {
  return { chain_event: entry.event, chain_block: entry.block, chain_index: entry.index, ...entry.data };
};

const mapPublishResultSuccess = (indexId: string, requestBlockRange: RequestBlockRange, result: Success) => {
  const events = result.events.map((evt) => {
    const meta = JSON.stringify({ create: { _index: indexId, _id: `${evt.block}_${evt.index}` } });
    const data = JSON.stringify(mapEntry(evt));
    return [meta, data].join('\n');
  });
  const eventsElasticPayload = events.join('\n') + '\n';
  return {
    Ok: [
      {
        indexPayload: eventsElasticPayload,
        blockRange: result.blockRange,
        requestBlockRange: mapRequestBlockRange(requestBlockRange),
      },
    ],
  };
};

const mapPublishResultErrorData = (result: Error) => {
  switch (result.error) {
    case 'empty-result':
      return { EmptyResult: [] };
    case 'limit-exceeded':
      return { LimitExceeded: [] };
    case 'unknown':
      return { Unknown: [] };
  }
};
const mapPublishResultError = (requestBlockRange: RequestBlockRange, result: Error) => {
  return {
    Error: [
      {
        data: mapPublishResultErrorData(result),
        blockRange: result.blockRange,
        requestBlockRange: mapRequestBlockRange(requestBlockRange),
      },
    ],
  };
};

const mapPublishResult = (indexId: string, requestBlockRange: RequestBlockRange, result: Result) => {
  switch (result.kind) {
    case 'Success':
      return mapPublishResultSuccess(indexId, requestBlockRange, result);
    case 'Error':
      return mapPublishResultError(requestBlockRange, result);
  }
};

export interface IScrapperActor {
  scrap(data: Data): Promise<boolean>;
}

export default class ScrapperActor extends AbstractActor implements IScrapperActor {
  private async invokeActor(actorType: string, actorMethod: string, payload: any) {
    const client = this.getDaprClient();

    const actorId = this.getActorId().getId();

    const url = `${client.daprHost}:${client.daprPort}/v1.0/actors/${actorType}/${actorId}/method/${actorMethod}`;

    try {
      const result = await axios.put(url, payload);

      const f = result.status === 200;
      if (!f) {
        console.log(result);
        console.error(`Call actor error ${url}`, result.data);
      }
      return f;
    } catch (ex) {
      console.log(ex);
      console.error(`Call actor error`);
      return false;
    }
  }

  private mapPublishPayload(data: Data, result: Result) {
    const actorId = this.getActorId().getId();
    const indexId = actorId.toLowerCase();
    return {
      contractAddress: data.contractAddress,
      abi: data.abi,
      ethProviderUrl: data.ethProviderUrl,
      result: mapPublishResult(indexId, data.blockRange, result),
    };
  }

  private async invokeDispatcherFailure(status: any) {
    const errorPayload = {
      AppId: { Scrapper: [] },
      Status: status,
    };

    const success = await this.invokeActor('scrapper-dispatcher', 'Failure', errorPayload);

    console.log('call to scrapper dispatcher Failure', success);
  }

  private async publishError(data: Data, result: Result) {
    if (result.kind === 'Error' && result.error === 'web3-failure') {
      // This is real api call failure
      await this.invokeDispatcherFailure({ ExternalServiceFailure: [`we3 call error: ${result.message}`] });
    } else {
      const payload = this.mapPublishPayload(data, result);
      const success = this.invokeActor('scrapper-dispatcher', 'Continue', payload);
      if (!success) {
        console.error('fail to invoke scrapper-dispatcher actor');
      }
    }
  }

  private async publishSuccess(data: Data, result: Success) {
    const _payload = this.mapPublishPayload(data, result);
    const payload = {
      ..._payload,
      result: (_payload.result as any).Ok[0],
    };

    const success = await this.invokeActor('scrapper-elastic-store', 'Store', payload);
    if (!success) {
      console.error('fail to invoke scrapper-elastic-store actor');
      await this.invokeDispatcherFailure({ CallChildActorFailure: [{ ElasticStore: [] }] });
      // const errorPayload = {
      //   AppId: { Scrapper: [] },
      //   Status: {
      //     CallChildActorFailure: [{ ElasticStore: [] }],
      //   },
      // };

      // const success = await this.invokeActor('scrapper-dispatcher', 'Failure', errorPayload);

      // console.log('call to scrapper dispatcher Failure', success);
    }
  }

  private async publish(data: Data, result: Result) {
    switch (result.kind) {
      case 'Success':
        return this.publishSuccess(data, result);
      case 'Error':
        return this.publishError(data, result);
    }
  }

  async _scrap(data: Data) {
    console.log('scrapper::scrap::start', data.blockRange);

    const abi = JSON.parse(data.abi);

    const handlerData: HandlerData = {
      ethProviderUrl: data.ethProviderUrl,
      contractAddress: data.contractAddress,
      abi,
      blockRange: data.blockRange,
    };

    const result = await handle(handlerData);

    console.log('scrapper::scrap::result', result.blockRange, result.kind === 'Success' ? 'Success' : result.error);

    const publishedResult = await this.publish(data, result);

    console.log('scrapper::scrap::publish', data.contractAddress);

    return publishedResult;
  }

  async scrap(data: Data) {
    this._scrap(data);
    return true;
  }
}
