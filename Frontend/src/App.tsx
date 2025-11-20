import { BrowserRouter } from "react-router-dom";
import AppRoutes from "./app/routes";
import Navigation from "./components/Navigation/Navigation";
import { ThemeProvider, createTheme } from "@mui/material/styles";
import CssBaseline from "@mui/material/CssBaseline";
import { Provider } from "react-redux";
import { store } from "./app/store/store";

const darkTheme = createTheme({
  palette: {
    mode: "dark",
  },
});

const App = () => {
  return (
    <Provider store={store}>
      <ThemeProvider theme={darkTheme}>
        <CssBaseline />
        <BrowserRouter>
          <Navigation />
          <main>
            <AppRoutes />
          </main>
        </BrowserRouter>
      </ThemeProvider>
    </Provider>
  );
};

export default App;
