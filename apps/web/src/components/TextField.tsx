import { useId, useState, type ComponentProps } from 'react'
import styles from './TextField.module.css'

interface TextFieldProps extends Omit<ComponentProps<'input'>, 'id' | 'className'> {
  label: string
  errors?: string[]
  hint?: string
}

export function TextField({ label, errors = [], hint, type = 'text', ...inputProps }: TextFieldProps) {
  const id = useId()
  const [revealed, setRevealed] = useState(false)
  const isPassword = type === 'password'
  const invalid = errors.length > 0
  const hasMessage = invalid || hint !== undefined

  return (
    <div className={styles.field}>
      <label htmlFor={id}>{label}</label>
      <div className={styles.control} data-invalid={invalid}>
        <input
          {...inputProps}
          id={id}
          type={isPassword && revealed ? 'text' : type}
          aria-invalid={invalid}
          aria-describedby={hasMessage ? `${id}-message` : undefined}
        />
        {isPassword && (
          <button
            type="button"
            className={styles.reveal}
            aria-label={`${revealed ? 'Hide' : 'Show'} ${label.toLowerCase()}`}
            onClick={() => setRevealed(!revealed)}
          >
            {revealed ? 'Hide' : 'Show'}
          </button>
        )}
      </div>
      {hasMessage && (
        <p id={`${id}-message`} className={invalid ? styles.error : styles.hint}>
          {invalid ? errors.join(' ') : hint}
        </p>
      )}
    </div>
  )
}
