// paged-response.model.ts
export interface PagedResponse<T> {
    data: T[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
  }




  