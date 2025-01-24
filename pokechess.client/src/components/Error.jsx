export default function Error({ error }) {
  return (
    <div className="flex h-screen w-screen items-center justify-center">
      <div className="text-center">
        <h1 className="mt-4 text-5xl font-semibold tracking-tight text-balance text-gray-900 sm:text-7xl">
          Something went wrong.
        </h1>

        <p className="mt-4">{error.name}</p>
        <p>{error.message}</p>
        <code>{error.stack}</code>
      </div>
    </div>
  );
}
