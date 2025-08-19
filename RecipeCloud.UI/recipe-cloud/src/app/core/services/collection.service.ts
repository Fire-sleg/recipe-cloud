import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { Collection } from "../models/collection.model";
import { environment } from "../../../environments/environment";

@Injectable({
  providedIn: 'root'
})
export class CollectionService {
  constructor(private http: HttpClient) {}

  createCollection(collection: FormData):Observable<Collection>{
    return this.http.post<Collection>(`${environment.apiUrl}/collections`, collection);
  }

  getByUserId(userId: string){
    return this.http.get<Collection[]>(`${environment.apiUrl}/collections/user/` + userId);
  }

}