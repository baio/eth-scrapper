const Web3 = require('web3');
const _ = require('lodash');

export interface Data {
  ethProviderUrl: string;
  contractAddress: string;
  abi: JSON;
  blockRange: { from?: number; to?: number };
}

export interface Entry {
  data: JSON;
  event: string;
  block: number;
  index: number;
}

export interface BlockRange {
  from: number;
  to: number;
}

export interface Success {
  kind: 'Success';
  blockRange: BlockRange;
  events: Entry[];
}

export interface Error {
  kind: 'Error';
  blockRange: BlockRange;
  error: 'unknown' | 'limit-exceeded' | 'empty-result' | 'web3-failure';
  message?: string;
}

export type Result = Success | Error;

const createContract = (web3: any, abi: any, address: string) => {
  const factoryContract = new web3.eth.Contract(abi, address);
  return factoryContract;
};

function read(contract: any, fromBlock: number, toBlock: number) {
  return contract.getPastEvents('allEvents', {
    fromBlock,
    toBlock,
  });
}

const cleanupEventValues = (vals: any) =>
  _.fromPairs(
    _.keys(vals)
      .filter((f: any) => _.isNaN(+f))
      .map((k: any) => [k, vals[k]]),
  );

// https://ethereum.stackexchange.com/questions/54967/how-to-get-only-past-2-days-events-using-getpastevents-everytime
export const handle = async (data: Data): Promise<Result> => {
  try {
    const web3Provider = new Web3.providers.HttpProvider(data.ethProviderUrl);

    const web3 = new Web3(web3Provider);

    const fromBlock = data.blockRange.from || 0;
    const toBlock = data.blockRange.to || (await web3.eth.getBlockNumber());

    const blockRange = {
      from: fromBlock,
      to: toBlock,
    };

    try {
      const contract = createContract(web3, data.abi, data.contractAddress);
      const result = await read(contract, blockRange.from, blockRange.to);

      const events = result.map((m: any) => ({
        data: cleanupEventValues(m.returnValues),
        event: m.event,
        block: m.blockNumber,
        index: m.logIndex,
      })) as Entry[];

      if (events.length > 0) {
        return {
          kind: 'Success',
          events,
          blockRange,
        };
      } else {
        return {
          kind: 'Error',
          error: 'empty-result',
          blockRange,
        };
      }
    } catch (err: any) {
      if (err && (err.message as string).indexOf('10000 results') !== -1) {
        return {
          kind: 'Error',
          error: 'limit-exceeded',
          blockRange,
        };
      } else {
        return {
          kind: 'Error',
          error: 'web3-failure',
          blockRange,
          message: err.message,
        };
      }
    }
  } catch (err: any) {
    return {
      kind: 'Error',
      error: 'web3-failure',
      blockRange: { from: 0, to: 0 },
      message: err.message,
    };
  }
};
