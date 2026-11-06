/** A project (from GET /api/projects). `code` is the route slug used to scope data. */
export interface IProject {
  id: number;
  code: string;
  name: string;
}
