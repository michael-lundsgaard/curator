import type { ComponentProps, ReactNode } from "react";

type FieldProps = {
  id: string;
  label: string;
  // Help below the input, read out with it.
  hint?: ReactNode;
  // Beside the label, on the right: "Forgot password?".
  aside?: ReactNode;
};

const inputClass = "rounded-[10px] border border-line bg-bg px-3 text-[15px] placeholder:text-meta";

// The id is also the name, which FormData reads.
export function TextField({
  id,
  label,
  hint,
  aside,
  ...input
}: FieldProps & ComponentProps<"input">) {
  return (
    <Field id={id} label={label} hint={hint} aside={aside}>
      <input
        id={id}
        name={id}
        aria-describedby={hint ? `${id}-hint` : undefined}
        className={`h-11 ${inputClass}`}
        {...input}
      />
    </Field>
  );
}

export function TextArea({
  id,
  label,
  hint,
  aside,
  ...textarea
}: FieldProps & ComponentProps<"textarea">) {
  return (
    <Field id={id} label={label} hint={hint} aside={aside}>
      <textarea
        id={id}
        name={id}
        aria-describedby={hint ? `${id}-hint` : undefined}
        className={`resize-y py-2.5 leading-normal ${inputClass}`}
        {...textarea}
      />
    </Field>
  );
}

function Field({ id, label, hint, aside, children }: FieldProps & { children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1.5">
      <div className="flex items-baseline justify-between">
        <label htmlFor={id} className="text-sm font-semibold">
          {label}
        </label>
        {aside}
      </div>
      {children}
      {hint && (
        <span id={`${id}-hint`} className="text-[13px] text-meta">
          {hint}
        </span>
      )}
    </div>
  );
}
