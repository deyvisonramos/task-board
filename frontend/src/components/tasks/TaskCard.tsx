import type { DragEvent } from 'react'
import type { TaskResponse } from '../../api/tasksApi'
import { formatDueDate } from './taskDate'

type TaskCardProps = {
  isSubmitting: boolean
  onDelete: (task: TaskResponse) => Promise<void>
  onDragEnd: () => void
  onDragStart: (task: TaskResponse) => void
  onEdit: (task: TaskResponse) => void
  task: TaskResponse
}

export function TaskCard({
  isSubmitting,
  onDelete,
  onDragEnd,
  onDragStart,
  onEdit,
  task,
}: TaskCardProps) {
  function handleDragStart(event: DragEvent<HTMLElement>) {
    event.dataTransfer.effectAllowed = 'move'
    event.dataTransfer.setData('text/plain', task.id)
    onDragStart(task)
  }

  return (
    <article
      className="task-card"
      draggable={!isSubmitting}
      onDragEnd={onDragEnd}
      onDragStart={handleDragStart}
    >
      <div className="task-card-header">
        <h3 className="task-card-title">{task.title}</h3>
        <span className="task-status-badge">{task.status}</span>
      </div>

      {task.description ? (
        <p className="task-card-description">{task.description}</p>
      ) : (
        <p className="task-card-description task-card-description-muted">
          No description.
        </p>
      )}

      <p className="task-card-meta">Due {formatDueDate(task.dueDate)}</p>

      <div className="task-card-actions">
        <button
          className="secondary-button"
          disabled={isSubmitting}
          onClick={() => onEdit(task)}
          type="button"
        >
          Edit
        </button>
        <button
          className="danger-button"
          disabled={isSubmitting}
          onClick={() => void onDelete(task)}
          type="button"
        >
          Delete
        </button>
      </div>
    </article>
  )
}
