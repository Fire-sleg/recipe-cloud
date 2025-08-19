// paged-response.model.ts
export interface PagedResponse<T> {
    data: T[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
  }


export class APIResponse<T = any> {
  statusCode!: number; // аналог HttpStatusCode (enum можна зробити окремо)
  isSuccess: boolean = true;
  errorsMessages: string[] = [];
  result!: T; // можна зробити generic для зручності
}

  