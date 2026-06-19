import { httpClient } from './httpClient'

export type TaskStatus = 'Todo' | 'InProgress' | 'Done'

export const TASK_STATUSES: TaskStatus[] = ['Todo', 'InProgress', 'Done']

export type TaskResponse = {
  id: string
  userId: string
  title: string
  description: string | null
  status: TaskStatus
  dueDate: string
  createdAt: string
  updatedAt: string
}

export type CreateTaskRequest = {
  title: string
  description: string | null
  status: TaskStatus
  dueDate: string
}

export type UpdateTaskRequest = CreateTaskRequest

export const tasksApi = {
  list: () => httpClient.get<TaskResponse[]>('/api/tasks'),

  get: (id: string) => httpClient.get<TaskResponse>(`/api/tasks/${id}`),

  create: (request: CreateTaskRequest) =>
    httpClient.post<TaskResponse>('/api/tasks', request),

  update: (id: string, request: UpdateTaskRequest) =>
    httpClient.put<TaskResponse>(`/api/tasks/${id}`, request),

  remove: (id: string) => httpClient.delete<void>(`/api/tasks/${id}`),
}
