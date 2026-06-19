import {
  TASK_STATUSES,
  type TaskResponse,
  type TaskStatus,
} from '../../api/tasksApi'
import { TaskCard } from './TaskCard'

type TaskListProps = {
  busyTaskId: string | null
  onDelete: (task: TaskResponse) => Promise<void>
  onDragEnd: () => void
  onDragStart: (task: TaskResponse) => void
  onEdit: (task: TaskResponse) => void
  onDropTask: (status: TaskStatus) => Promise<void>
  onStatusChange: (task: TaskResponse, status: TaskStatus) => Promise<void>
  tasks: TaskResponse[]
}

const STATUS_LABELS: Record<TaskStatus, string> = {
  Todo: 'Todo',
  InProgress: 'In progress',
  Done: 'Done',
}

export function TaskList({
  busyTaskId,
  onDelete,
  onDragEnd,
  onDragStart,
  onEdit,
  onDropTask,
  onStatusChange,
  tasks,
}: TaskListProps) {
  if (tasks.length === 0) {
    return (
      <div className="task-empty-state">
        <h2>No tasks yet</h2>
        <p>Create your first task to start filling the board.</p>
      </div>
    )
  }

  return (
    <div className="task-board" aria-label="Task board">
      {TASK_STATUSES.map((status) => {
        const statusTasks = tasks.filter((task) => task.status === status)

        return (
          <section className="task-column" key={status}>
            <div className="task-column-header">
              <h2>{STATUS_LABELS[status]}</h2>
              <span>{statusTasks.length}</span>
            </div>

            {statusTasks.length > 0 ? (
              <div
                className="task-column-list"
                onDragOver={(event) => event.preventDefault()}
                onDrop={(event) => {
                  event.preventDefault()
                  void onDropTask(status)
                }}
              >
                {statusTasks.map((task) => (
                  <TaskCard
                    isSubmitting={busyTaskId === task.id}
                    key={task.id}
                    onDelete={onDelete}
                    onDragEnd={onDragEnd}
                    onDragStart={onDragStart}
                    onEdit={onEdit}
                    onStatusChange={onStatusChange}
                    task={task}
                  />
                ))}
              </div>
            ) : (
              <div
                className="task-column-empty"
                onDragOver={(event) => event.preventDefault()}
                onDrop={(event) => {
                  event.preventDefault()
                  void onDropTask(status)
                }}
              >
                No tasks in this status.
              </div>
            )}
          </section>
        )
      })}
    </div>
  )
}
