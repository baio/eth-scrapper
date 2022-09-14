import { DataAccessConfig } from '../features/dataAcess';

const appConfig = {
  api: {
    baseUrl: process.env.NEXT_PUBLIC_BASE_URL || 'http://localhost:5005',
    daprAppId: process.env.NEXT_PUBLIC_DAPR_APP_ID || 'scrapper-api',
  } as DataAccessConfig,
  stateChangesQueryInterval: (+(process.env.NEXT_PUBLIC_STATE_CHANGES_QUERY_INTERVAL || 0) || 60) * 1000,
};

export default appConfig;
