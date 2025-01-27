import { useState } from "react";
import Header from "../Header";
import Card from "../Card";
import Button from "../ButtonPage";

export default function NamePage({
  connection,
  setGameStatus,
  setError,
  name,
  setName,
}) {
  const [formStatus, setFormStatus] = useState("waiting");

  function onSubmit(e) {
    e.preventDefault();

    setFormStatus("loading");

    connection.invoke("PlayerJoined", name).catch((err) => {
      setGameStatus("error");
      setError(err);
    });
  }
  return (
    <div className="flex h-screen flex-col items-center justify-center bg-indigo-900">
      <Header>Poke-Chess</Header>
      <Card>
        <h3 className="text-base font-semibold text-gray-900">
          Enter your name.
        </h3>
        <form className="mt-5 sm:flex sm:items-center" onSubmit={onSubmit}>
          <div className="w-full sm:max-w-xs">
            <input
              id="name"
              name="name"
              placeholder="Johnny"
              autoFocus
              required
              onChange={(e) => setName(e.target.value)}
              className="block w-full rounded-md bg-white px-3 py-1.5 text-base text-gray-900 outline outline-1 -outline-offset-1 outline-gray-300 placeholder:text-gray-400 focus:outline focus:outline-2 focus:-outline-offset-2 focus:outline-indigo-600 sm:text-sm/6"
            />
          </div>
          <Button loading={formStatus === "loading"}>Play</Button>
        </form>
      </Card>
    </div>
  );
}
