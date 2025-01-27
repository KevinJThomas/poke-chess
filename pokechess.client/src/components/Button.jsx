import clsx from "clsx";
export default function Button({ children, className, disabled, ...props }) {
  return (
    <button
      {...props}
      disabled={disabled}
      type="button"
      className={clsx(
        "w-16 rounded bg-white px-2 py-1 text-xs font-semibold text-gray-900 ring-1 shadow-sm ring-gray-300 ring-inset hover:bg-gray-50",
        disabled && "cursor-not-allowed opacity-50",
        !disabled && "cursor-pointer",
        className,
      )}
    >
      {children}
    </button>
  );
}
