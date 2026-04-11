export class PagedRequest {
  pageNumber: number = 1;
  pageSize: number = 10;
}

export class PagedResponse<T> {
  data: T[] = [];
  pageNumber: number = 1;
  pageSize: number = 10;
  totalRecords: number = 0;
  totalPages: number = 0;
}
