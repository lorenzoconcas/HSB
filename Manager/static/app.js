//remember to compile these scripts with babel
"use strict";


class Main extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            logged: false
        };
    }
    onLogin(data) {
        //  sessionStorage.setItem("user", JSON.stringify(data))
        this.setState({ logged: data.logged });

    }
    onLogout() {
        //  sessionStorage.removeItem("user");
        this.setState({ logged: false });
    }
    render() {
        return <Login key="LoginPage" onLogin={(data) => this.onLogin(data)} />

        //return this.state.logged ?
        // <HomePage key="HomePage" onLogout={() => this.onLogout()} /> :
        //<Login key="LoginPage" onLogin={(data) => this.onLogin(data)} />
    }
}

ReactDOM.render(
    <Main />,
    document.getElementById('root')
);
