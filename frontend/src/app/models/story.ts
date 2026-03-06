export interface Story {
  id: number;
  title: string;
  url: string | null;
  time: number;
  by: string;
}

export interface StoriesResponse {
  stories: Story[];
  totalCount: number;
  page: number;
  pageSize: number;
}
