export interface AppConfig {
  ethProviderUrl: string | null;
}

export interface AppState {
  config: AppConfig;
  ethBlockNumber: number;
}
