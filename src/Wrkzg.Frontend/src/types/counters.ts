export interface Counter {
  id: number;
  name: string;
  value: number;
  trigger: string;
  responseTemplate: string;
  createdAt: string;
}

export interface CreateCounterRequest {
  name: string;
  value?: number;
  responseTemplate?: string;
}

export interface UpdateCounterRequest {
  name?: string;
  value?: number;
  responseTemplate?: string;
}
