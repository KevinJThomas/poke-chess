import Spinner from "./Spinner";
import clsx from "clsx";

export default function Button({
  children,
  loading,
  fullWidth = false,
  classes,
  disabled,
  ...props
}) {
  const buttonClasses = clsx(
    "mt-3 inline-flex w-full items-center justify-center rounded-md bg-pink-600 px-3 py-2 text-sm font-semibold text-white shadow-sm hover:bg-pink-400 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600",
    !fullWidth && "sm:ml-3 sm:mt-0 sm:w-auto",
    (disabled || loading) && "opacity-50 cursor-not-allowed",
    classes,
  );
  return (
    <button
      type="submit"
      className={buttonClasses}
      disabled={disabled || loading}
      {...props}
    >
      <div className="inline-flex items-center justify-center">
        {loading ? <Spinner /> : children}
      </div>
    </button>
  );
}
