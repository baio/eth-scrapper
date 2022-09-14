import Web3 from 'web3';
import { Result } from '../sharedModels';

export const getEthBlockNumber = async (ethProviderUrl: string): Promise<Result<number, string>> => {
  try {
    var web3 = new Web3(ethProviderUrl);
    const result = await web3.eth.getBlockNumber();
    return { kind: 'ok', value: result };
  } catch (err: any) {
    console.error(err);
    return { kind: 'error', error: err.message };
  }
};

export const storeEthProviderUrl = async (ethProviderUrl: string) => {
  localStorage.setItem('ethProviderUrl', ethProviderUrl);
};

export const getEthProviderUrl = async () => {
  return localStorage.getItem('ethProviderUrl');
};
