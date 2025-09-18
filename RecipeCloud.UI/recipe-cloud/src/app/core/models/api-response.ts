export class APIResponse<T = any> {
  statusCode!: number; 
  isSuccess: boolean = true;
  errorMessages: string[] = [];
  result!: T; 

  constructor (obj: T){
    this.result = obj;

  }
}